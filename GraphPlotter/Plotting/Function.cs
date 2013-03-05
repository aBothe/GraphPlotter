using D_Parser.Dom.Expressions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

	class Function : INotifyPropertyChanged
	{
		static int colorCounter = 0;
		public static List<Color> DefaultColors = new List<Color> { Colors.Red, Colors.Blue, Colors.Green, Colors.Orange, Colors.Teal };

		#region Properties
		IExpression expression;
		string name;
		Color graphColor;
		System.Reflection.Emit.DynamicMethod calcMethod;

		public IExpression Expression
		{
			get { return expression; }
			private set
			{
				expression = value;
				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("Expression"));
			}
		}
		public string Name {
			get { return name; }
			set {
				name = value;
				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("Name"));
			}
		}
		public Color GraphColor
		{
			get { return graphColor; }
			set
			{
				graphColor = value;
				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("GraphColor"));
			}
		}
		public System.Reflection.Emit.DynamicMethod CompiledExpression
		{
			get { return calcMethod; }
			set
			{
				calcMethod = value;
				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("CompiledExpression"));
			}
		}
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

			return new Function(x, name) { GraphColor = DefaultColors[colorCounter++], CompiledExpression = dm };
		}

		protected Function(IExpression x, string name)
		{
			Name = name;
			Expression = x;
		}

		#endregion

		public double Calculate(double x)
		{
			return (double)CompiledExpression.Invoke(null, new[] { (object)x });
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
