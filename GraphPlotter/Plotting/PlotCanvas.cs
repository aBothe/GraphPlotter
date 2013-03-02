using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xwt;
using Xwt.Drawing;

namespace GraphPlotter.Plotting
{
	class PlotCanvas : Canvas
	{
		#region Properties
		public readonly List<Function> Graphs = new List<Function>();
		public Rectangle AxisBoundaries = new Rectangle(-Math.PI, -1, Math.PI*4, 2);

		public double CalculationDensity = 4;
		#endregion

		public PlotCanvas()
		{
			Graphs.Add(Function.Parse("x * x", "f"));
			Graphs.Add(Function.Parse("x^^2", "g"));
		}

		protected override void OnDraw(Context ctxt, Rectangle dirtyRect)
		{
			ctxt.Save();

			// Draw the axes

			// Draw the labels
			

			// Draw the graphs
			var x_min = AxisBoundaries.X;
			var x_max = AxisBoundaries.X + AxisBoundaries.Width + CalculationDensity;
			var x_delta = (AxisBoundaries.Width * CalculationDensity) / dirtyRect.Width;

			var y_multiplier = -(dirtyRect.Height / AxisBoundaries.Height);
			var y=0d;
			var y_min = AxisBoundaries.Y;
			var y_max = AxisBoundaries.Y + AxisBoundaries.Height;

			ctxt.Translate(0, dirtyRect.Height/2);
			foreach (var f in Graphs)
			{
				y = 0d;
				ctxt.SetColor(f.GraphColor);
				ctxt.MoveTo(0, 0);
				var px = -CalculationDensity;
				for (var x = x_min; x <= x_max; x += x_delta)
				{
					px += CalculationDensity;

					try
					{
						y = f.Calculate(x);

						if (y < y_min)
						{
							y = y_min;
						}
						else if (y > y_max)
							y = y_max;
						else
						{
							ctxt.LineTo(px, y * y_multiplier);
							continue;
						}
					}
					catch (DivideByZeroException ex)
					{
					}

					ctxt.MoveTo(px, y * y_multiplier);
				}

				ctxt.Stroke();
			}

			

			ctxt.Restore();
			base.OnDraw(ctxt, dirtyRect);
		}

		void DrawXAxis(Context ctxt)
		{

		}

		void DrawYAxis(Context ctxt)
		{

		}
	}
}
