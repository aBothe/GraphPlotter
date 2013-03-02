using D_Parser.Dom.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xwt.Drawing;

namespace GraphPlotter.Plotting
{
	class InvalidExpressionException : Exception
	{
		public InvalidExpressionException(string invalidStringPart)
			: base("Invalid expression: " + invalidStringPart)
		{
		}
	}

	class Function
	{
		static int colorCounter = 0;
		public static List<Color> DefaultColors = new List<Color> { Colors.Red, Colors.Blue, Colors.Green, Colors.Orange, Colors.Teal };

		#region Properties
		public IExpression Expression {get; private set;}
		public string Name;
		public Color GraphColor;
		public System.Reflection.Emit.DynamicMethod calcMethod;
		#endregion

		#region Init
		public static Function Parse(string expression, string name)
		{
			var x = D_Parser.Parser.DParser.ParseExpression(expression);

			if (x == null)
				throw new InvalidExpressionException(expression);

			var dm = ExpressionCompiler.Compile(name, x);

			if (colorCounter >= DefaultColors.Count)
				colorCounter = 0;
			if (DefaultColors.Count == 0)
				DefaultColors.Add(Colors.Black);

			return new Function(x, name) { GraphColor = DefaultColors[colorCounter++], calcMethod = dm };
		}

		protected Function(IExpression x, string name)
		{
			Name = name;
			Expression = x;
		}

		#endregion

		public double Calculate(double x)
		{
			return (double)calcMethod.Invoke(null, new[] { (object)x });
		}
	}
}
