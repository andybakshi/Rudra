namespace ExpRT.Core
{
    internal static class OperatorSD
    {

        record OperatorDef(List<string?> Operators, OpType OperatorType, int Precedence, Assoc Associativity);

        static readonly List<OperatorDef> ops = new()
        {
            // Reference https://en.cppreference.com/w/cpp/language/operator_precedence
            new OperatorDef(new List < string ? >() { "!", "u+", "u-"}, OpType.Unary, 3, Assoc.RTL), // to distinguish unary "+" / "-" from binary "+" / "-"
            new OperatorDef(new List < string ? >() { "*", "/", "%" }, OpType.Binary, 5, Assoc.LTR),
            new OperatorDef(new List < string ? >() { "+", "-" }, OpType.Binary, 6, Assoc.LTR),
            new OperatorDef(new List < string ? >() { ">", ">=", "<", "<=" }, OpType.Binary, 9, Assoc.LTR),
            new OperatorDef(new List < string ? >() { "=", "==", "!=" }, OpType.Binary, 10, Assoc.LTR), // as for user equal is "="
            new OperatorDef(new List < string ? >() { "AND", "&&" }, OpType.Binary, 14, Assoc.LTR),
            new OperatorDef(new List < string ? >() { "OR", "||" }, OpType.Binary, 15, Assoc.LTR)
        };

        internal static Assoc GetAssociativity(string op)
        {
            return ops.First(f => f.Operators.Contains(op)).Associativity;
        }

        internal static int GetPrecedence(string op)
        {
            return ops.First(f => f.Operators.Contains(op)).Precedence;
        }

        internal static OpType GetOperatorType(string op)
        {
            return ops.First(f => f.Operators.Contains(op)).OperatorType;
        }


        static readonly string[]? seps = { "==", "!=", ">=", "<=", "&&", "||", "!", ">", "<", "(", ")", " AND ", " OR ", "=", "*", "+", "-", "/" };

        internal static ReadOnlySpan<string> GetSeparatorsAsSpan()
        {
            return seps.AsSpan();
        }

        internal enum OpType
        {
            Unary,
            Binary
        }

        internal enum Assoc
        {
            RTL,
            LTR
        }

    }
}
