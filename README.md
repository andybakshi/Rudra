
# Rudra 

**Rudra** is a .NET Runtime Expression Builder library that allows developers to create queries at runtime that are not predefined or statically known at compile time.



## Features

- **Dynamic Expression Building**: Create expressions at runtime from human-readable string or list of nodes, enabling flexible and powerful querying capabilities.

- **Dynamic Type Handling**: Supports dynamic type checking and allows for implicit and explicit casting of field datatypes. This feature ensures that any comparable types can be processed without hard-coded type logic.

- **Integration with LINQ**: The generated expressions can be easily integrated with LINQ queries to filter collections dynamically.


## Supported Operations

#### Unary Operations

| Operator | Description         | Example     |
|----------|---------------------|-------------|
| `!`      | Logical NOT         | `!a`        |
| `+`      | Unary Plus          | `+a`        |
| `-`      | Unary Minus         | `-a`        |

#### Binary Operations

##### Arithmetic Operations

| Operator | Description      | Example     |
|----------|------------------|-------------|
| `*`      | Multiplication   | `a * b`     |
| `/`      | Division         | `a / b`     |
| `%`      | Modulus          | `a % b`     |
| `+`      | Addition         | `a + b`     |
| `-`      | Subtraction      | `a - b`     |

##### Relational Operations

| Operator | Description              | Example     |
|----------|--------------------------|-------------|
| `>`      | Greater than             | `a > b`     |
| `>=`     | Greater than or equal to | `a >= b`    |
| `<`      | Less than                | `a < b`     |
| `<=`     | Less than or equal to    | `a <= b`    |
| `=`      | Equality                 | `a = b`     |
| `==`     | Equality                 | `a == b`    |
| `!=`     | Inequality               | `a != b`    |

##### Logical Operations

| Operator | Description         | Example                 |
|----------|---------------------|-------------------------|
| `AND`    | Logical AND         | `a AND b`   |
| `OR`     | Logical OR          | `a OR b`    |
| `&&`     | Logical AND         | `a && b`    |
| `\|\|`   | Logical OR          | `a \|\| b`  |

## Installation

Rudra is designed for ASP.NET Core and it targets `net6.0`, `net7.0` and `net8.0`.

Install [Rudra package](https://www.nuget.org/packages/Rudra) using Nuget Package Manager:

```powershell
Install-Package Rudra
```

Or via .NET CLI:
```shell
dotnet add package Rudra
```



## Usage and Examples

Suppose you have an object definition for `Student` :

```csharp
public enum Gender
{
    Other = 0,
    Male = 1,
    Female = 2
}

public class Student
{
    public int StudentID { get; set; }
    public string? Name { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public DateTime? EnrollmentDate { get; set; }
    public bool IsActive { get; set; }
    public decimal? GPA { get; set; }
    public decimal? TuitionBalance { get; set; }
    public DateTime? GraduationDate { get; set; }
    public bool IsInternational { get; set; }
    public decimal StandardGPA { get; set; }
    public int CreditsEarned { get; set; }
}
```
#### Query Definition
Query can be defined in two ways: 

* Either as a single string 
* As a list of constituent parts (field, operator, and value).
```csharp
string query = "gender = Female";
List<string> nodes = new() { "Gender", "=", "female" }; 
```
> N.B.
> Fields are Case Insensitive 

#### Runtime Expression Builder
To create a runtime expression easily, you can use the static class `ExpressionBuilder`. Call the `BuildFilterExpression` method and pass the `Student` object as the generic type parameter.

`BuildFilterExpression` method accepts both `sring` and `List<string>` and returns LINQ Expression.

```csharp
using Rudra;

// Creating Filter Expression from single string type (query)
var filterExp_query = ExpressionBuilder.BuildFilterExpression<Student>(query);

// Creating Filter Expression from list of constituent parts (nodes)
var filterExp_nodes = ExpressionBuilder.BuildFilterExpression<Student>(nodes);
```

#### Getting Results

Use the *filterExp* returned by `BuildFilterExpression` .

```csharp
/// In memory List
List<Student> students;
var result = students.AsQueryable().Where(filterExp).ToList();

/// SQL Server
// Using Entity Framework
public class RudraContext : DbContext
{
    public DbSet<Student> Students { get; set; }
    public RudraContext()
    {
    }
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlServer(@"ConnectionString");
    }

}
// Now use Context
using (var db = new RudraContext())
{
    var result = db.Students.Where(filterExp).ToList();
} 
```

#### Dynamic Query Examples
```csharp
// Compare decimal? with int fields, it will cast int dynamically to decimal? to work on all scenarios
// CreditsEarned - int field
// TuitionBalance - decimal? field
string query = "CreditsEarned > TuitionBalance";

// Logical AND (Students who are international or have a GPA percentage less than 60%)
string query = "IsInternational = true OR GPA * 10 < 60";

// Logical OR (Students who are active and have a GPA percentage greater than 85%)
string query = "IsActive = true AND GPA * 10 > 85";

// Arithmatic (like checking 20% of tuition balance is more than 500)
string query = "TuitionBalance * (20/100) > 500";

// Compare fields
string query = "GraduationDate <= EnrollmentDate";

// negative value
string query = "TuitionBalance < -100";

// Boolean Type
string query = "IsActive = true";

// For DateTime type use single quote ('')
string query = "GraduationDate > '2021-05-01'";

```


## License

Copyright (c) 2024 Abhishek Bakshi

Rudra is licensed under the MIT License. See the [LICENSE](./LICENSE.txt) file for details.



