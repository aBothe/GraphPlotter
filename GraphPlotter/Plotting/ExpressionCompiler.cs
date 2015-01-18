using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using D_Parser.Dom.Expressions;
using D_Parser.Parser;
using System.Reflection;

namespace GraphPlotter.Plotting
{
	public class ExpressionCompiler : D_Parser.Dom.ExpressionVisitor
	{
		ILGenerator ilGen;
		static MethodInfo MathPow;
		static Dictionary<string, MethodInfo> SingleArgMathFunctions = new Dictionary<string, MethodInfo>();
		Dictionary<string, double> Constants = new Dictionary<string, double>();

		static Dictionary<ulong, DynamicMethod> CachedMethods = new Dictionary<ulong, DynamicMethod>();

		static ExpressionCompiler()
		{
			var math = typeof(Math);
			var dbl = typeof(double);
			MathPow = math.GetMethod("Pow", new[]{ dbl,dbl });
			
			var dblA = new[] { dbl };
			SingleArgMathFunctions["sin"] = math.GetMethod("Sin", dblA);
			SingleArgMathFunctions["tan"] = math.GetMethod("Tan", dblA);
			SingleArgMathFunctions["cos"] = math.GetMethod("Cos", dblA);
			SingleArgMathFunctions["abs"] = math.GetMethod("Abs", dblA);
			SingleArgMathFunctions["ln"] = math.GetMethod("Log", dblA);
			SingleArgMathFunctions["log"] = math.GetMethod("Log10", dblA);
			SingleArgMathFunctions["sqrt"] = math.GetMethod("Sqrt", dblA);
		}

		static int methodNum = 1;
		public static DynamicMethod Compile(IExpression x)
		{
			if (x == null)
				throw new ArgumentNullException("Expression must not be empty!");

			DynamicMethod dm;
			if (CachedMethods.TryGetValue(x.GetHash(), out dm))
				return dm;

			dm = new DynamicMethod("_dynamicMethod"+(methodNum++), typeof(double), new[] { typeof(double) });

			Generate(x, dm.GetILGenerator());

			return dm;
		}

		public static void Generate(IExpression x, ILGenerator gen)
		{
			x.Accept(new ExpressionCompiler { ilGen = gen });
			gen.Emit(OpCodes.Ret);
		}

		public void Visit(ConditionalExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(OrOrExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(AndAndExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(XorExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(OrExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(AndExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(RelExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(ShiftExpression x)
		{
			throw new NotImplementedException();
		}

		public void EmitMathOpCode(byte opToken)
		{
			switch (opToken)
			{
					// Math ops
				case DTokens.Plus:
					ilGen.Emit(OpCodes.Add);
					break;
				case DTokens.Minus:
					ilGen.Emit(OpCodes.Sub);
					break;
				case DTokens.Times:
					ilGen.Emit(OpCodes.Mul);
					break;
				case DTokens.Div:
					ilGen.Emit(OpCodes.Div);
					break;
				case DTokens.Pow:
					ilGen.EmitCall(OpCodes.Call, MathPow, null);
					break;
				case DTokens.Mod:
					ilGen.Emit(OpCodes.Rem);
					break;

					// Bin ops

					// Rel ops
				case DTokens.GreaterEqual:
					ilGen.Emit(OpCodes.Clt);
					ilGen.Emit(OpCodes.Not);
					break;
				case DTokens.GreaterThan:
					ilGen.Emit(OpCodes.Cgt);
					break;
				case DTokens.LessThan:
					ilGen.Emit(OpCodes.Clt);
					break;
				case DTokens.LessEqual:
					ilGen.Emit(OpCodes.Cgt);
					ilGen.Emit(OpCodes.Not);
					break;
				default:
					throw new ArgumentException("Invalid opToken (" + DTokens.GetTokenString(opToken) + ")");
			}
		}


		void VisitOperatorExpression(OperatorBasedExpression x)
		{
			double d;
			VisitOperatorExpression(x, out d);
		}

		/// <summary>
		/// Returns true if the expression is a constant expression.
		/// False if the expression has to be evaluated run-time
		/// </summary>
		/// <param name="opEx"></param>
		/// <param name="constant"></param>
		/// <returns></returns>
		bool VisitOperatorExpression(OperatorBasedExpression opEx, out double constant, bool emit = true)
		{
			constant = 0;
			bool leftConst, rightConst;
			double lv = 0, rv = 0;
			leftConst = TryEvalConstExpression(opEx.LeftOperand, out lv);

			bool leftValueAlreadyPushed = false;
			bool affectedByAssociativity =	opEx.OperatorToken == DTokens.Times || 
											opEx.OperatorToken == DTokens.Div;

			OperatorBasedExpression sx;
			if (affectedByAssociativity && opEx.RightOperand is OperatorBasedExpression)
			{
				sx = opEx.RightOperand as OperatorBasedExpression;

				rightConst = TryEvalConstExpression(sx.LeftOperand, out rv);

				if (leftConst && rightConst)
				{
					lv = ConstMathOp(lv, rv, opEx.OperatorToken);
				}
				else if(!emit)
					return false;
				else
				{
					if (leftConst)
						ilGen.Emit(OpCodes.Ldc_R8, lv);
					else
						opEx.LeftOperand.Accept(this);

					if (rightConst)
						ilGen.Emit(OpCodes.Ldc_R8, rv);
					else
						sx.LeftOperand.Accept(this);

					EmitMathOpCode(opEx.OperatorToken);
					leftValueAlreadyPushed = true;
				}
			}
			else
				sx = opEx;

			rightConst = TryEvalConstExpression(sx.RightOperand, out rv);

			if (leftConst && rightConst)
			{
				constant = ConstMathOp(lv, rv, sx.OperatorToken);
				return true;
			}
			else if (!emit)
				return false;

			if (!leftValueAlreadyPushed)
			{
				if (leftConst)
					ilGen.Emit(OpCodes.Ldc_R8, lv);
				else
					opEx.LeftOperand.Accept(this);
			}

			if (rightConst)
				ilGen.Emit(OpCodes.Ldc_R8, rv);
			else
				opEx.RightOperand.Accept(this);

			EmitMathOpCode(opEx.OperatorToken);
			return false;
		}

		bool TryEvalConstExpression(IExpression x, out double result)
		{
			result = 0;
			return	(x is OperatorBasedExpression &&
					VisitOperatorExpression(x as OperatorBasedExpression, out result, false)) ||
					(x is IdentifierExpression &&
					TryEvalConstIdentifier(x as IdentifierExpression, out result));
		}

		double ConstMathOp(double lv, double rv, byte opToken)
		{
			switch (opToken)
			{
				case DTokens.Plus:
					return lv + rv;
				case DTokens.Minus:
					return lv - rv;
				case DTokens.Times:
					return lv * rv;
				case DTokens.Div:
					return lv / rv;
				case DTokens.Pow:
					return Math.Pow(lv, rv);
				case DTokens.Mod:
					return lv % rv;
				default:
					throw new InvalidOperationException("Invalid math op ("+DTokens.GetTokenString(opToken)+")");
			}
		}

		public void Visit(AddExpression x)
		{
			VisitOperatorExpression(x);
		}

		public void Visit(MulExpression x)
		{
			VisitOperatorExpression(x);
		}

		public void Visit(CatExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(PowExpression x)
		{
			VisitOperatorExpression(x);
		}

		public void Visit(UnaryExpression_And x)
		{
			throw new NotImplementedException();
		}

		public void Visit(UnaryExpression_Increment x)
		{
			throw new NotImplementedException();
		}

		public void Visit(UnaryExpression_Decrement x)
		{
			throw new NotImplementedException();
		}

		public void Visit(UnaryExpression_Add x)
		{
			x.UnaryExpression.Accept(this);
		}

		public void Visit(UnaryExpression_Sub x)
		{
			x.UnaryExpression.Accept(this);
			ilGen.Emit(OpCodes.Neg);
		}

		public void Visit(UnaryExpression_Not x)
		{
			throw new NotImplementedException();
		}

		public void Visit(UnaryExpression_Cat x)
		{
			throw new NotImplementedException();
		}

		public void Visit(PostfixExpression_Increment x)
		{
			throw new NotImplementedException();
		}

		public void Visit(PostfixExpression_Decrement x)
		{
			throw new NotImplementedException();
		}

		public void Visit(PostfixExpression_MethodCall x)
		{
			if (x.Arguments == null || x.ArgumentCount != 1)
				throw new ArgumentException("Currently there are only functions with exactly one argument supported!");

			if (x.PostfixForeExpression is IdentifierExpression)
			{
				var idx = x.PostfixForeExpression as IdentifierExpression;

				if (!idx.IsIdentifier)
					throw new ArgumentException("Method Identifier expected.");

				MethodInfo mi;
				if (!SingleArgMathFunctions.TryGetValue(idx.StringValue.ToLower(), out mi))
				{
					var sb = new StringBuilder("Unknown method. Only ");

					foreach (var k in SingleArgMathFunctions.Keys)
						sb.Append(k).Append(",");

					sb.Remove(sb.Length - 1, 1);

					sb.Append(" will be accepted!");

					throw new ArgumentException(sb.ToString());
				}

				x.Arguments[0].Accept(this);

				ilGen.EmitCall(OpCodes.Call, mi, null);
				return;
			}

			throw new ArgumentException("Single identifier as method expected, not "+x.PostfixForeExpression.ToString());
		}

		public bool TryEvalConstIdentifier(IdentifierExpression x, out double d)
		{
			d = 0;
			if(x.IsIdentifier)
			{
				var id = x.StringValue;
				if (id == "x")
					return false;
				else if (Constants.TryGetValue(id, out d))
					return true;
				else if (id == "e")
					d = Math.E;
				else if (id.ToLower() == "pi")
					d = Math.PI;
				else
					throw new InvalidOperationException("Unknown symbol " + id);
			}
			else if ((x.Format & LiteralFormat.Scalar) != 0)
				d = (double)(decimal)x.Value;
			else
				throw new InvalidOperationException("Invalid literal!");

			return true;
		}

		public void Visit(IdentifierExpression x)
		{
			if (x.IsIdentifier && x.StringValue == "x")
				ilGen.Emit(OpCodes.Ldarg_0);
			else
			{
				double d;
				TryEvalConstIdentifier(x, out d);
				ilGen.Emit(OpCodes.Ldc_R8, d);
			}
		}

		public void Visit(TokenExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(SurroundingParenthesesExpression x)
		{
			x.Expression.Accept(this);
		}

		#region Not supported

		public void Visit(Expression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(AssignExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(EqualExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(IdentityExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(InExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(UnaryExpression_Mul x)
		{
			throw new NotImplementedException();
		}

		public void Visit(UnaryExpression_Type x)
		{
			throw new NotImplementedException();
		}

		public void Visit(UnaryExpression_SegmentBase x)
		{
			throw new NotImplementedException ();
		}

		public void Visit(NewExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(AnonymousClassExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(DeleteExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(CastExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(PostfixExpression_Access x)
		{
			throw new NotImplementedException();
		}

		public void Visit(PostfixExpression_ArrayAccess x)
		{
			throw new NotImplementedException();
		}

		public void Visit(TemplateInstanceExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(TypeDeclarationExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(ArrayLiteralExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(AssocArrayExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(FunctionLiteral x)
		{
			throw new NotImplementedException();
		}

		public void Visit(AssertExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(MixinExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(ImportExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(TypeidExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(IsExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(TraitsExpression x)
		{
			throw new NotImplementedException();
		}

		public void Visit(TraitsArgument arg)
		{
			throw new NotImplementedException();
		}

		public void Visit(VoidInitializer x)
		{
			throw new NotImplementedException();
		}

		public void Visit(ArrayInitializer x)
		{
			throw new NotImplementedException();
		}

		public void Visit(StructInitializer x)
		{
			throw new NotImplementedException();
		}

		public void Visit(StructMemberInitializer structMemberInitializer)
		{
			throw new NotImplementedException();
		}

		public void Visit(AsmRegisterExpression x)
		{
			throw new NotImplementedException ();
		}
		#endregion
	}
}
