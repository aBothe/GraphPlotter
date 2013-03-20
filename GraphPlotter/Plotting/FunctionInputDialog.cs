using D_Parser.Dom.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xwt;
using Xwt.Drawing;

namespace GraphPlotter.Plotting
{
	class FunctionInputDialog : Dialog
	{
		static int nameCounter = 0;
		public static List<string> DefaultNames = new List<string> { "f", "g", "h", "p", "q", "u", "v", "y", "z" };

		public Function Function { get; private set; }
		TextEntry entry_Name;
		TextEntry entry_Expression;
		IExpression parsedExpression;

		Label messageLabel = new Label();

		public FunctionInputDialog(Function f = null)
		{
			Title = (f == null ? "Create new":"Edit ") + " function";
			Function = f;

			this.Buttons.Add(Command.Ok);
			this.Buttons.Add(Command.Cancel);

			var vb = new VBox { MinWidth = 300, MinHeight= 150 };
			Content = vb;

			var l = new Label("Function name");
			var labelFont = l.Font.WithWeight(Xwt.Drawing.FontWeight.Bold);
			l.Font = labelFont;
			vb.PackStart(l);

			entry_Name = new TextEntry();
			if (f != null)
				entry_Name.Text = f.Name;
			else
			{
				if (nameCounter >= DefaultNames.Count)
					nameCounter = 0;
				entry_Name.Text = DefaultNames[nameCounter];
			}
			vb.PackStart(entry_Name, BoxMode.Fill);


			vb.PackStart(new Label("Function term") { Font = labelFont });

			entry_Expression = new TextEntry();
			entry_Expression.Changed += entry_Expression_Changed;
			if (f != null)
				entry_Expression.Text = f.Expression.ToString();
			else
			{
				entry_Expression.Text = "x";
			}
			vb.PackStart(entry_Expression, BoxMode.Fill);

			
			vb.PackStart(messageLabel, BoxMode.FillAndExpand);
		}

		void entry_Expression_Changed(object sender, EventArgs e)
		{
			string errMsg = null;
			parsedExpression = Function.ParseExpressionString(entry_Expression.Text, out errMsg);
			
			if(errMsg != null)
			{
				messageLabel.TextColor = Colors.Red;
				messageLabel.Text = errMsg;
				return;
			}

			messageLabel.TextColor = Colors.Green;
			messageLabel.Text = "Expression is valid!";
		}

		public bool ValidateAndWriteChanges()
		{
			if (string.IsNullOrWhiteSpace(entry_Name.Text))
			{
				MessageDialog.ShowError("Enter a function name first!");
			}

			if (parsedExpression == null ||
				messageLabel.TextColor == Colors.Red)
			{
				MessageDialog.ShowError("Enter a correct function expression first!");
				return false;
			}

			System.Reflection.Emit.DynamicMethod dm;
			try
			{
				dm = ExpressionCompiler.Compile(parsedExpression);
			}
			catch (Exception ex)
			{
				MessageDialog.ShowError("Error during compiling the expression",ex.Message);
				return false;
			}

			if (Function == null)
			{
				if (DefaultNames[nameCounter] == entry_Name.Text)
					nameCounter++;

				Function = new Function();
			}

			Function.Name = entry_Name.Text;
			Function.UpdateExpression(parsedExpression, dm);

			return true;
		}


		protected override void OnCommandActivated(Command cmd)
		{
			if (cmd != Command.Ok || ValidateAndWriteChanges())
				base.OnCommandActivated(cmd);
		}
	}
}
