using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lamn.Compiler
{
	class AST
	{
		public interface StatementVisitor
		{
			void Visit(LocalAssignmentStatement statement);
			void Visit(LocalFunctionStatement statement);
			void Visit(IfStatement statement);
			void Visit(WhileStatement statement);
			void Visit(DoStatement statement);
			void Visit(ForStatement statement);
			void Visit(FunctionCallStatement statement);
			void Visit(AssignmentStatement statement);
			void Visit(RepeatStatement statement);
			void Visit(ReturnStatement statement);
			void Visit(BreakStatement statement);
			void Visit(FunctionStatement statement);
		}

		public interface ExpressionVisitor
		{
			void Visit(NameExpression expression);
			void Visit(NumberExpression expression);
			void Visit(StringExpression expression);
			void Visit(BoolExpression expression);
			void Visit(NilExpression expression);
			void Visit(VarArgsExpression expression);
			void Visit(UnOpExpression expression);
			void Visit(BinOpExpression expression);
			void Visit(FunctionExpression expression);
			void Visit(LookupExpression expression);
			void Visit(SelfLookupExpression expression);
			void Visit(IndexExpression expression);
			void Visit(FunctionApplicationExpression expression);
			void Visit(Constructor expression);
		}

		public class Chunk
		{
			public List<Statement> Statements { get; private set; }

			public Chunk(List<Statement> statements)
			{
				Statements = statements;
			}
		}

		public abstract class Statement {
			public abstract void Visit(StatementVisitor visitor);
		}

		public class LocalAssignmentStatement : Statement
		{
			public List<String> Variables { get; private set; }
			public List<Expression> Expressions { get; private set; }

			public LocalAssignmentStatement(List<String> variables, List<Expression> expressions)
			{
				Variables = variables;
				Expressions = expressions;
			}

			public override void Visit(StatementVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class LocalFunctionStatement : Statement
		{
			public String Name { get; private set; }
			public Body Body { get; private set; }

			public LocalFunctionStatement(String name,Body body)
			{
				Name = name;
				Body = body;
			}

			public override void Visit(StatementVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class IfStatement : Statement
		{
			public List<TestBlock> Conditions { get; private set; }

			public IfStatement(List<TestBlock> conditions)
			{
				Conditions = conditions;
			}

			public override void Visit(StatementVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class WhileStatement : Statement
		{
			public Expression Condition { get; private set; }
			public Chunk Block { get; private set; }

			public WhileStatement(Expression condition, Chunk block)
			{
				Condition = condition;
				Block = block;
			}

			public override void Visit(StatementVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class DoStatement : Statement
		{
			public Chunk Block { get; private set; }

			public DoStatement(Chunk block)
			{
				Block = block;
			}

			public override void Visit(StatementVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class ForStatement : Statement
		{
			public ForClause Clause { get; private set; }
			public Chunk Block { get; private set; }

			public ForStatement(ForClause clause, Chunk block)
			{
				Clause = clause;
				Block = block;
			}

			public override void Visit(StatementVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class FunctionCallStatement : Statement
		{
			public Expression Expr { get; private set; }

			public FunctionCallStatement(Expression expr)
			{
				Expr = expr;
			}

			public override void Visit(StatementVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class AssignmentStatement : Statement
		{
			public List<Expression> Variables { get; private set; }
			public List<Expression> Expressions { get; private set; }

			public AssignmentStatement(List<Expression> variables, List<Expression> expressions)
			{
				Variables = variables;
				Expressions = expressions;
			}

			public override void Visit(StatementVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class ForClause { }

		public class NumForClause : ForClause
		{
			public String Name { get; private set; }
			public Expression Expr1 { get; private set; }
			public Expression Expr2 { get; private set; }
			public Expression Expr3 { get; private set; }

			public NumForClause(String name, Expression expr1, Expression expr2, Expression expr3)
			{
				Name = name;
				Expr1 = expr1;
				Expr2 = expr2;
				Expr3 = expr3;
			}
		}

		public class ListForClause : ForClause
		{
			public List<String> Names { get; private set; }
			public List<Expression> Expressions { get; private set; }

			public ListForClause(List<String> names, List<Expression> expressions)
			{
				Names = names;
				Expressions = expressions;
			}
		}

		public class RepeatStatement : Statement
		{
			public Chunk Block { get; private set; }
			public Expression Condition { get; private set; }

			public RepeatStatement(Chunk block, Expression condition) {
				Block = block;
				Condition = condition;
			}

			public override void Visit(StatementVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class ReturnStatement : Statement
		{
			public List<Expression> Expressions;

			public ReturnStatement(List<Expression> expressions)
			{
				Expressions = expressions;

				if (Expressions == null)
				{
					Expressions = new List<Expression>();
				}
			}

			public override void Visit(StatementVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class BreakStatement : Statement {
			public override void Visit(StatementVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class FunctionStatement : Statement
		{
			public String MainName { get; private set; }
			public List<String> FieldNames { get; private set; }
			public String SelfName { get; private set; }
			public Body Body { get; private set; }

			public FunctionStatement(String mainName, List<String> fieldNames, String selfName, Body body)
			{
				MainName = mainName;
				FieldNames = fieldNames;
				SelfName = selfName;
				Body = body;
			}

			public override void Visit(StatementVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class FunctionParamList
		{
			public List<String> NamedParams { get; private set; }
			public bool HasVarArgs { get; private set; }

			public FunctionParamList(List<String> parameters, bool hasVarArgs)
			{
				NamedParams = parameters;
				HasVarArgs = hasVarArgs;
			}
		}

		public class TestBlock
		{
			public Expression Cond { get; private set; }
			public Chunk Block { get; private set; }

			public TestBlock(Expression cond, Chunk block)
			{
				Cond = cond;
				Block = block;
			}
		}

		public class Body
		{
			public FunctionParamList ParamList { get; private set; }
			public Chunk Chunk { get; private set; }

			public Body(FunctionParamList p, Chunk c)
			{
				ParamList = p;
				Chunk = c;
			}
		}

		public abstract class Expression {
			public abstract void Visit(ExpressionVisitor visitor);
		}

		public class NameExpression : Expression
		{
			public String Value { get; private set; }

			public NameExpression(String value)
			{
				Value = value;
			}

			public override void Visit(ExpressionVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class NumberExpression : Expression
		{
			public double Value { get; private set; }

			public NumberExpression(double value)
			{
				Value = value;
			}

			public override void Visit(ExpressionVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class StringExpression : Expression
		{
			public String Value { get; private set; }

			public StringExpression(String value)
			{
				Value = value;
			}

			public override void Visit(ExpressionVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class BoolExpression : Expression
		{
			public bool Value { get; private set; }

			public BoolExpression(bool value)
			{
				Value = value;
			}

			public override void Visit(ExpressionVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class NilExpression : Expression
		{
			public override void Visit(ExpressionVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class VarArgsExpression : Expression
		{
			public override void Visit(ExpressionVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class UnOpExpression : Expression
		{
			public String Op { get; private set; }
			public Expression Expr { get; private set; }

			public UnOpExpression(String op, Expression expr)
			{
				Op = op;
				Expr = expr;
			}

			public override void Visit(ExpressionVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class BinOpExpression : Expression
		{
			public String Op { get; private set; }
			public Expression LeftExpr { get; private set; }
			public Expression RightExpr { get; private set; }

			public BinOpExpression(String op, Expression leftExpr, Expression rightExpr)
			{
				Op = op;
				LeftExpr = leftExpr;
				RightExpr = rightExpr;
			}

			public override void Visit(ExpressionVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class FunctionExpression : Expression
		{
			public Body Body { get; private set; }

			public FunctionExpression(Body body)
			{
				Body = body;
			}

			public override void Visit(ExpressionVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class LookupExpression : Expression
		{
			public Expression Obj { get; private set; }
			public String Name { get; private set; }

			public LookupExpression(Expression obj, String name)
			{
				Obj = obj;
				Name = name;
			}

			public override void Visit(ExpressionVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class SelfLookupExpression : Expression
		{
			public Expression Obj { get; private set; }
			public String Name { get; private set; }

			public SelfLookupExpression(Expression obj, String name)
			{
				Obj = obj;
				Name = name;
			}

			public override void Visit(ExpressionVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class IndexExpression : Expression
		{
			public Expression Obj { get; private set; }
			public Expression Index { get; private set; }

			public IndexExpression(Expression obj, Expression index)
			{
				Obj = obj;
				Index = index;
			}

			public override void Visit(ExpressionVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class FunctionApplicationExpression : Expression
		{
			public Expression Obj { get; private set; }
			public List<AST.Expression> Args { get; private set; }

			public FunctionApplicationExpression(Expression obj, List<AST.Expression> args)
			{
				Obj = obj;
				Args = args;
			}

			public override void Visit(ExpressionVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class Constructor : Expression
		{
			public List<ConField> Fields { get; private set; }

			public Constructor(List<ConField> fields)
			{
				Fields = fields;
			}

			public override void Visit(ExpressionVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public class ConField { }

		public class ListField : ConField { 
			public Expression Expr { get; private set; }

			public ListField(Expression expr)
			{
				Expr = expr;
			}
		}

		public class NameRecField : ConField {
			public String Name { get; private set; }
			public Expression Value { get; private set; }

			public NameRecField(String name, Expression value)
			{
				Name = name;
				Value = value;
			}
		}

		public class ExprRecField : ConField { 
			public Expression IndexExpr { get; private set; }
			public Expression Value { get; private set; }

			public ExprRecField(Expression indexExpr, Expression value)
			{
				IndexExpr = indexExpr;
				Value = value;
			}
		}

		public Chunk Contents { get; private set; }

		public AST(Chunk chunk)
		{
			Contents = chunk;
		}
	}
}
