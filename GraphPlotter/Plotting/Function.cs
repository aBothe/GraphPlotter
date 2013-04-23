using D_Parser.Dom.Expressions;
using D_Parser.Parser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml;
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
			string errText;
			var x = ParseExpressionString(expression, out errText);
			if (errText != null)
				throw new InvalidExpressionException(errText);
			f.UpdateExpression(x);
			return f;
		}

		public static Function LoadFrom(XmlReader x)
		{
			var f = new Function();

			while (x.Read())
			{
				switch (x.LocalName)
				{
					case "Visible":
						f.visible = x.ReadString().ToLower() == "true";
						break;
					case "Name":
						f.name = x.ReadString();
						break;
					case "Expression":
						try
						{
							string errText;
							var expression = ParseExpressionString(x.ReadString(), out errText);

							if (errText == null && expression != null)
							{
								f.UpdateExpression(expression);
								continue;
							}

							Xwt.MessageDialog.ShowError("Error while parsing function expression", errText);
						}
						catch(Exception ex) {
							Xwt.MessageDialog.ShowError("Error while parsing function expression", ex.Message);
						}

						while (x.Read()) ;
						return null;
				}
			}
			return f;
		}

		public void SaveTo(XmlWriter x)
		{
			x.WriteElementString("Visible", visible.ToString());

			x.WriteStartElement("Name");
			x.WriteCData(name);
			x.WriteEndElement();

			x.WriteStartElement("Expression");
			x.WriteCData(expression.ToString());
			x.WriteEndElement();
		}
		#endregion

		public static IExpression ParseExpressionString(string expressionString, out string errText)
		{
			errText = null;

			if (string.IsNullOrWhiteSpace(expressionString))
			{
				errText = "Given expression must not be empty!";
				return null;
			}

			using (var sr = new System.IO.StringReader(expressionString))
			{
				var p = DParser.Create(sr);
				p.Step();
				var x = p.Expression();

				if (p.ParseErrors.Count > 0)
					errText = "Column " + p.ParseErrors[0].Location.Column + ": " + p.ParseErrors[0].Message;

				return x;
			}
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

		object[] calcArgs = new[] { (object)0.0 };
		public double Calculate(double x)
		{
			calcArgs[0] = x;
			try
			{
				return (double)CompiledExpression.Invoke(null, calcArgs);
			}
			catch { return double.NaN; }
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
