using System.Linq.Expressions;
using System.Reflection;
using static Rudra.Core.OperatorSD;

namespace Rudra.Core
{
    internal static class ExpressionBuilderCore
    {
        private record RawData { internal object data = new(); }
        private record RawConstant { internal object constant = new(); }
        private record RawUnaryExpression { internal string op = String.Empty; internal RawData rawData = new(); }
        private record RawBinaryExpression { internal string op = String.Empty; internal RawData leftRawData = new(); internal RawData rightRawData = new(); }

        internal static Expression BuildExpression(string query, ParameterExpression parameterExp, Type? type = default)
        {
            query = "(" + query + ")";
            ReadOnlySpan<char> source = query.AsSpan();

            ReadOnlySpan<string> separators = GetSeparatorsAsSpan();

            Stack<string> stackOperator = new();
            Stack<object> stackOperand = new();

            string field = "";
            bool isSep = false;
            bool wasSep = false;

            try
            {

                for (int i = 0; i < source.Length; i++)
                {
                    if (source[i] == '\'')
                    {
                        while (source[++i] != '\'')
                        {
                            field += source[i];
                        }
                        continue;
                    }
                    foreach (string separator in separators)
                    {
                        int currentSepLength = separator.Length;
                        if (source[i] == separator[0] && currentSepLength <= source.Length - i)
                        {
                            if (currentSepLength == 1 || source.Slice(i, currentSepLength).SequenceEqual(separator))
                            {
                                isSep = true;

                                string trimmedSep = separator.Trim();

                                // unary checking

                                if (wasSep && (trimmedSep == "+" || trimmedSep == "-"))
                                {
                                    stackOperator.Push("u" + trimmedSep);
                                    break;
                                }

                                if (!string.IsNullOrEmpty(field.Trim()))
                                {
                                    PushFieldToOperandStack(field.Trim(), parameterExp, stackOperand);
                                }

                                if (trimmedSep == "(")
                                {
                                    stackOperator.Push(trimmedSep);
                                }
                                else if (trimmedSep != ")")
                                {
                                    while (stackOperator.Count != 0 && stackOperator.Peek() != "("
                                        && (
                                            (GetAssociativity(stackOperator.Peek()) == Assoc.LTR && GetPrecedence(stackOperator.Peek()) <= GetPrecedence(trimmedSep))
                                            || (GetAssociativity(stackOperator.Peek()) == Assoc.RTL && GetPrecedence(stackOperator.Peek()) < GetPrecedence(trimmedSep))
                                           )
                                          )
                                    {
                                        string op = stackOperator.Pop();
                                        EvaluateAndPushToOperandStack(op, stackOperand);
                                    }

                                    stackOperator.Push(trimmedSep);
                                }
                                else if (trimmedSep == ")")
                                {
                                    while (stackOperator.Count != 0 && stackOperator.Peek() != "(")
                                    {
                                        string op = stackOperator.Pop();
                                        EvaluateAndPushToOperandStack(op, stackOperand);
                                    }
                                    stackOperator.Pop();
                                }

                                i += currentSepLength - 1;
                                field = "";
                                break;
                            }
                        }
                    }
                    if (!isSep)
                    {
                        field += source[i];
                    }
                    wasSep = source[i] == ' ' ? wasSep : isSep;
                    isSep = false;
                }

                if (stackOperand.Count == 1 && stackOperator.Count == 0)
                {
                    switch (stackOperand.Peek())
                    {
                        case Expression expression:
                            {
                                stackOperand.Pop();
                                // if type exists then convert expression and then return
                                if (type != default)
                                {
                                    return Expression.Convert(expression, type);
                                }
                                else
                                {
                                    return expression;
                                }
                            }

                        case RawData rawData:
                            {
                                stackOperand.Pop();

                                // if type exists then build expression and then return
                                if (type != default)
                                {
                                    return EvaluateExpressionOfRawData(rawData, type);
                                }
                                else
                                {
                                    throw new InvalidDataException(message: "Provide at least one valid property in query or Type to convert");
                                }
                            }

                        default:
                            stackOperand.Pop();
                            throw new InvalidOperationException();
                    }
                }
                else
                {
                    throw new NotSupportedException("Query Not Supported");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        internal static Expression BuildExpression(List<string> nodes, ParameterExpression parameterExp, Type? type = default)
        {
            nodes.Insert(0, "(");
            nodes.Insert(nodes.Count, ")");

            ReadOnlySpan<string> separators = GetSeparatorsAsSpan();

            Stack<string> stackOperator = new();
            Stack<object> stackOperand = new();

            bool isSep = false;
            bool wasSep = false;

            try
            {

                foreach (string node in nodes)
                {
                    foreach (string separator in separators)
                    {
                        string trimmedSep = separator.Trim();
                        if (trimmedSep == node)
                        {

                            isSep = true;

                            // unary checking

                            if (wasSep && (trimmedSep == "+" || trimmedSep == "-"))
                            {
                                stackOperator.Push("u" + trimmedSep);
                                break;
                            }

                            if (trimmedSep == "(")
                            {
                                stackOperator.Push(trimmedSep);
                            }
                            else if (trimmedSep != ")")
                            {
                                while (stackOperator.Count != 0 && stackOperator.Peek() != "("
                                    && (
                                        (GetAssociativity(stackOperator.Peek()) == Assoc.LTR && GetPrecedence(stackOperator.Peek()) <= GetPrecedence(trimmedSep))
                                        || (GetAssociativity(stackOperator.Peek()) == Assoc.RTL && GetPrecedence(stackOperator.Peek()) < GetPrecedence(trimmedSep))
                                       )
                                      )
                                {
                                    string op = stackOperator.Pop();
                                    EvaluateAndPushToOperandStack(op, stackOperand);
                                }

                                stackOperator.Push(trimmedSep);
                            }
                            else if (trimmedSep == ")")
                            {
                                while (stackOperator.Count != 0 && stackOperator.Peek() != "(")
                                {
                                    string op = stackOperator.Pop();
                                    EvaluateAndPushToOperandStack(op, stackOperand);

                                }
                                stackOperator.Pop();
                            }

                            break;
                        }
                    }
                    if (!isSep)
                    {
                        PushFieldToOperandStack(node, parameterExp, stackOperand);
                    }
                    wasSep = isSep;
                    isSep = false;
                }

                if (stackOperand.Count == 1 && stackOperator.Count == 0)
                {
                    switch (stackOperand.Peek())
                    {
                        case Expression expression:
                            {
                                stackOperand.Pop();
                                // if type exists then convert expression and then return
                                if (type != default)
                                {
                                    return Expression.Convert(expression, type);
                                }
                                else
                                {
                                    return expression;
                                }
                            }

                        case RawData rawData:
                            {
                                stackOperand.Pop();

                                // if type exists then build expression and then return
                                if (type != default)
                                {
                                    return EvaluateExpressionOfRawData(rawData, type);
                                }
                                else
                                {
                                    throw new InvalidDataException(message: "Provide at least one valid property in query or Type to convert");
                                }
                            }

                        default:
                            stackOperand.Pop();
                            throw new InvalidOperationException();
                    }
                }
                else
                {
                    throw new NotSupportedException("Query Not Supported");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        static void PushFieldToOperandStack(string field, ParameterExpression parameterExp, Stack<object> stackOperand)
        {
            var propertyInfo = parameterExp.Type.GetProperty(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);

            switch (propertyInfo)
            {
                case null:
                    {
                        RawConstant rawConstant = new() { constant = field };
                        stackOperand.Push(new RawData() { data = rawConstant });
                        break;
                    }

                default:
                    {
                        stackOperand.Push(Expression.Property(parameterExp, propertyInfo));
                    }
                    break;
            }
        }

        static void EvaluateAndPushToOperandStack(string op, Stack<object> stackOperand)
        {
            try
            {
                switch (GetOperatorType(op))
                {
                    case OpType.Unary:
                        {
                            switch (stackOperand.Pop())
                            {
                                case RawData rawData:
                                    {
                                        RawData evaluatedRawData = new() { data = GetRawUnaryExpression(op, rawData) };
                                        stackOperand.Push(evaluatedRawData);
                                    }
                                    break;

                                case Expression expression:
                                    {
                                        Expression evaluatedExpression = GetUnaryExpression(op, expression);
                                        stackOperand.Push(evaluatedExpression);
                                    }
                                    break;

                                default:
                                    {
                                        throw new NotSupportedException("Invalid Data in Operand Stack");
                                    }
                            }
                        }
                        break;

                    case OpType.Binary:
                        {
                            switch (stackOperand.Pop(), stackOperand.Pop())
                            {
                                case (RawData rightRawData, RawData leftRawData):
                                    {
                                        RawData evaluatedRawData = new() { data = GetRawBinaryExpression(op, leftRawData, rightRawData) };
                                        stackOperand.Push(evaluatedRawData);
                                    }
                                    break;

                                case (Expression rightExpression, Expression leftExpression):
                                    {
                                        // build Expression with Resultant Type

                                        Type resultantType = GetResultantType(rightExpression.Type, leftExpression.Type);

                                        if (rightExpression.Type != resultantType)
                                        {
                                            rightExpression = Expression.Convert(rightExpression, resultantType);
                                        }

                                        if (leftExpression.Type != resultantType)
                                        {
                                            leftExpression = Expression.Convert(leftExpression, resultantType);
                                        }

                                        Expression evaluatedExpression = GetBinaryExpression(op, leftExpression, rightExpression);
                                        stackOperand.Push(evaluatedExpression);
                                    }
                                    break;

                                case (RawData rightRawData, Expression leftExpression):
                                    {
                                        Type type = leftExpression.Type;
                                        Expression rightExpression = EvaluateExpressionOfRawData(rightRawData, type);

                                        Expression evaluatedExpression = GetBinaryExpression(op, leftExpression, rightExpression);
                                        stackOperand.Push(evaluatedExpression);
                                    }
                                    break;

                                case (Expression rightExpression, RawData leftRawData):
                                    {
                                        Type type = rightExpression.Type;
                                        Expression leftExpression = EvaluateExpressionOfRawData(leftRawData, type);

                                        Expression evaluatedExpression = GetBinaryExpression(op, leftExpression, rightExpression);
                                        stackOperand.Push(evaluatedExpression);
                                    }
                                    break;

                                default:
                                    {
                                        throw new NotSupportedException("Invalid Data in Operand Stack");
                                    }
                            }
                        }
                        break;

                    default:
                        {
                            throw new NotSupportedException("Invalid Operator in Operator Stack");
                        }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        static RawUnaryExpression GetRawUnaryExpression(string op, RawData rawData)
        {
            return new RawUnaryExpression()
            {
                op = op,
                rawData = rawData
            };
        }

        static RawBinaryExpression GetRawBinaryExpression(string op, RawData leftRawData, RawData rightRawData)
        {
            return new RawBinaryExpression()
            {
                op = op,
                leftRawData = leftRawData,
                rightRawData = rightRawData
            };
        }

        static Expression GetUnaryExpression(string op, Expression operandExpression)
        {
            return op switch
            {
                "u+" => operandExpression,
                "u-" => Expression.Negate(operandExpression),
                "!" => Expression.Not(operandExpression),
                _ => throw new NotSupportedException($"The unary operator '{op.Replace("u", "")}' is not supported")
            };
        }

        static Expression GetBinaryExpression(string op, Expression leftExpression, Expression rightExpression)
        {
            return op switch
            {
                "+" => Expression.Add(leftExpression, rightExpression),
                "-" => Expression.Subtract(leftExpression, rightExpression),
                "*" => Expression.Multiply(leftExpression, rightExpression),
                "/" => Expression.Divide(leftExpression, rightExpression),
                ">" => Expression.GreaterThan(leftExpression, rightExpression),
                ">=" => Expression.GreaterThanOrEqual(leftExpression, rightExpression),
                "<" => Expression.LessThan(leftExpression, rightExpression),
                "<=" => Expression.LessThanOrEqual(leftExpression, rightExpression),
                "==" => Expression.Equal(leftExpression, rightExpression),
                "!=" => Expression.NotEqual(leftExpression, rightExpression),
                "AND" => Expression.AndAlso(leftExpression, rightExpression),
                "OR" => Expression.OrElse(leftExpression, rightExpression),
                "&&" => Expression.AndAlso(leftExpression, rightExpression),
                "||" => Expression.OrElse(leftExpression, rightExpression),
                "=" => Expression.Equal(leftExpression, rightExpression),
                _ => throw new NotSupportedException($"The binary operator '{op}' is not supported")
            };
        }

        static Expression EvaluateExpressionOfRawData(RawData rawData, Type type)
        {
            switch (rawData.data)
            {
                case RawConstant rawConstant:
                    {
                        try
                        {
                            object constant = rawConstant.constant;

                            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {
                                Type nullType = type;
                                type = type.GetGenericArguments()[0];

                                if (type.IsEnum)
                                {
                                    constant = Enum.Parse(type, (string)constant, true);
                                }
                                else
                                {
                                    constant = Convert.ChangeType(constant, type);
                                }

                                return Expression.Constant(constant, nullType);
                            }
                            else
                            {
                                if (type.IsEnum)
                                {
                                    constant = Enum.Parse(type, (string)constant, true);
                                }
                                else
                                {
                                    constant = Convert.ChangeType(constant, type);
                                }

                                return Expression.Constant(constant);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new NotSupportedException("RawData Type Mismatch", ex);
                        }
                    }

                case RawUnaryExpression rawUnaryExpression:
                    {
                        Expression operandExpression = EvaluateExpressionOfRawData(rawUnaryExpression.rawData, type);
                        return GetUnaryExpression(rawUnaryExpression.op, operandExpression);
                    }

                case RawBinaryExpression rawBinaryExpression:
                    {
                        Expression leftExpression = EvaluateExpressionOfRawData(rawBinaryExpression.leftRawData, type);
                        Expression rightExpression = EvaluateExpressionOfRawData(rawBinaryExpression.rightRawData, type);
                        return GetBinaryExpression(rawBinaryExpression.op, leftExpression, rightExpression);
                    }

                default:
                    {
                        throw new NotSupportedException("Invalid Data in Operand Stack");
                    }
            }
        }

        static Type GetResultantType(Type type1, Type type2)
        {
            switch (type1.IsGenericType && type1.GetGenericTypeDefinition() == typeof(Nullable<>),
                    type2.IsGenericType && type2.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                case (true, true)
                    when Type.GetTypeCode(type1.GetGenericArguments()[0]) == Type.GetTypeCode(type2.GetGenericArguments()[0]):

                case (true, true)
                    when Type.GetTypeCode(type1.GetGenericArguments()[0]) > Type.GetTypeCode(type2.GetGenericArguments()[0]):
                    {
                        return type1;
                    }

                case (true, true)
                    when Type.GetTypeCode(type1.GetGenericArguments()[0]) < Type.GetTypeCode(type2.GetGenericArguments()[0]):
                    {
                        return type2;
                    }


                case (true, false)
                    when Type.GetTypeCode(type1.GetGenericArguments()[0]) == Type.GetTypeCode(type2):

                case (true, false)
                    when Type.GetTypeCode(type1.GetGenericArguments()[0]) > Type.GetTypeCode(type2):
                    {
                        return type1;
                    }

                case (true, false)
                    when Type.GetTypeCode(type1.GetGenericArguments()[0]) < Type.GetTypeCode(type2):
                    {
                        return type2;
                    }



                case (false, true)
                    when Type.GetTypeCode(type1) == Type.GetTypeCode(type2.GetGenericArguments()[0]):

                case (false, true)
                    when Type.GetTypeCode(type1) > Type.GetTypeCode(type2.GetGenericArguments()[0]):
                    {
                        return type1;
                    }

                case (false, true)
                    when Type.GetTypeCode(type1) < Type.GetTypeCode(type2.GetGenericArguments()[0]):
                    {
                        return type2;
                    }



                case (false, false)
                    when Type.GetTypeCode(type1) == Type.GetTypeCode(type2):

                case (false, false)
                    when Type.GetTypeCode(type1) > Type.GetTypeCode(type2):
                    {
                        return type1;
                    }

                case (false, false)
                    when Type.GetTypeCode(type1) < Type.GetTypeCode(type2):
                    {
                        return type2;
                    }


                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }
    }
}
