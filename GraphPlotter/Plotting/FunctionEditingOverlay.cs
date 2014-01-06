using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Xwt;
using Xwt.Drawing;

namespace GraphPlotter.Plotting
{
	class FunctionEditingOverlay : Widget
	{
		public readonly PlotCanvas Plot;
		Button addFuncButton;
		VBox funcEntryBox;

		public FunctionEditingOverlay(PlotCanvas plot)
		{
			Plot = plot;

			MarginRight = 10;

			plot.Options.Functions.CollectionChanged += Functions_CollectionChanged;

			var mainVBox = new VBox();
			Content = mainVBox;

			funcEntryBox = new VBox();
			mainVBox.PackStart(funcEntryBox, BoxMode.Fill);

			var footerHBox = new HBox();
			mainVBox.PackStart(footerHBox, BoxMode.Fill);

			addFuncButton = new Button(StockIcons.Add.WithSize(16)) { TooltipText = "Add new function" };
			addFuncButton.Clicked += (s, e) => { 
				var fid = new FunctionInputDialog();
				if (fid.Run(ParentWindow) == Command.Ok)
					plot.Options.Functions.Add(fid.Function);
			};
			footerHBox.PackEnd(addFuncButton, BoxMode.None);
		}

		FunctionEntryWidget GetFuncEntryAt(int i)
		{
			return funcEntryBox.Children.ElementAt(i) as FunctionEntryWidget;
		}

		FunctionEntryWidget GetFuncEntryAt(Function f)
		{
			return funcEntryBox.Children.FirstOrDefault((Widget w) => (w as FunctionEntryWidget).Function == f) as FunctionEntryWidget;
		}

		void Functions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (Function f in e.NewItems)
					{
						funcEntryBox.PackEnd(new FunctionEntryWidget(Plot,f), BoxMode.FillAndExpand);
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					foreach (Function f in e.OldItems)
					{
						var entry = GetFuncEntryAt(f);
						funcEntryBox.Remove(entry);
					}
					break;
				case NotifyCollectionChangedAction.Reset:
					funcEntryBox.Clear();
					break;
				default:
					throw new NotImplementedException("Other list operations are not implemented yet");
			}

			if (Plot.Options.Functions.Count >= PlotCanvasOptions.MaximumFunctionCount)
			{
				addFuncButton.Sensitive = false;
				addFuncButton.Visible = false;
			}
			else
			{
				addFuncButton.Sensitive = true;
				addFuncButton.Visible = true;
			}
		}

		class FunctionEntryWidget : HBox
		{
			public readonly Function Function;
			Label funcText;

			public FunctionEntryWidget(PlotCanvas plot,Function f)
			{
				this.Function = f;
				f.PropertyChanged += f_PropertyChanged;

				funcText = new Label(f.Name + " = " + f.Expression.ToString()) { 
					TextColor = f.GraphColor, 
					TextAlignment = Alignment.Start, 
					MarginLeft=5,
					MarginTop=3
				};
				PackStart(funcText, BoxMode.FillAndExpand);

				var m = new Menu();
				PackEnd(new MenuButton((string)null){ TooltipText = "Edit function", WidthRequest = 23, Menu = m });

				var cb = new CheckBoxMenuItem("Visible") { Checked = Function.Visible };
				m.Items.Add(cb);
				cb.Clicked += (s, e) => Function.Visible = cb.Checked;

				var mb = new MenuItem("Edit");
				m.Items.Add(mb);
				mb.Clicked += (s, e) => new FunctionInputDialog(Function).Run(ParentWindow);

				mb = new MenuItem("Remove");
				m.Items.Add(mb);
				mb.Clicked += (s, e) => plot.Options.Functions.Remove(Function);
			}

			void f_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
			{
				switch (e.PropertyName)
				{
					case "Expression":
						funcText.Text = Function.Name + " = " + (Function.Expression != null ? Function.Expression.ToString() : "");
						break;
					case "GraphColor":
						funcText.TextColor = Function.GraphColor;
						break;
				}
			}
		}
	}
}
