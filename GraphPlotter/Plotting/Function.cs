using D_Parser.Dom.Expressions;
using D_Parser.Parser;
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
		bool visible = true;
		string name;
		Color graphColor;
		System.Reflection.Emit.DynamicMethod calcMethod;

		public IExpression Expression
		{
			get { return expression; }
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
		public bool Visible
		{
			get { return visible; }
			set
			{
				visible = value;
				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("Visible"));
			}
		}
		public System.Reflection.Emit.DynamicMethod CompiledExpression
		{
			get { return calcMethod; }
		}
		#endregion

		#region Init
		public Function()
		{
			if (colorCounter >= DefaultColors.Count)
				colorCounter = 0;
			if (DefaultColors.Count == 0)
				DefaultColors.Add(Colors.Black);
			GraphColor = DefaultColors[colorCounter++];
		}

		public static Function Parse(string name, string expression)
		{
			var f = new Function();
			f.Name = name;
			f.UpdateExpression(expression);
			return f;
		}
		#endregion

		/// <summary>
		/// May throws an exception when compilation errors occur.
		/// </summary>
		public void UpdateExpression(string expressionString)
		{
			if (string.IsNullOrWhiteSpace(expressionString))
				throw new ArgumentNullException("Given expression must not be empty!");

			var p = DParser.Create(new System.IO.StringReader(expressionString));
			p.Step();
			var x = p.Expression();

			if (p.ParseErrors.Count > 0)
				throw new ArgumentException("Column "+p.ParseErrors[0].Location.Column+ ": "+p.ParseErrors[0].Message);

			UpdateExpression(x);
		}

		/// <summary>
		/// May throws an exception when compilation errors occur.
		/// </summary>
		public void UpdateExpression(IExpression x, System.Reflection.Emit.DynamicMethod compilate = null)
		{
			expression = x;
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs("Expression"));

			calcMethod = compilate ?? ExpressionCompiler.Compile(x);

			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs("CompiledExpression"));
		}

		public double Calculate(double x)
		{
			return (double)CompiledExpression.Invoke(null, new[] { (object)x });
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
