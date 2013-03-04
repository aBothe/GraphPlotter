using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Xwt;
using Xwt.Backends;
using Xwt.Drawing;

namespace GraphPlotter.Plotting
{
	class PlotCanvas : Canvas
	{
		#region Properties
		Function[] graphs;
		Function[] Graphs
		{
			get { return graphs; }
			set
			{
				graphs = value;
				Redraw();
			}
		}

		/// <summary>
		/// The mathematical point to take for the upper left corner.
		/// </summary>
		public Point BaseLocation;
		public double Scale_X;
		public double Scale_Y;

		public double MovingDelta;
		public double ScalingDelta;

		public const double DotsPerCentimeter = 40; // ISSUE: Assume 96 as actual (dots per inch) number - It's windows standard

		double DeltaX { get { return CalculationDensity / (DotsPerCentimeter * Scale_X); } }
		double YMultiplier { get { return DotsPerCentimeter * Scale_Y; } }

		public int CalculationDensity;
		public double TickDensity_XAxis;
		public double TickDensity_YAxis;

		public readonly TextLayout tickLabelFont;
		public Color axisColor;
		public Color gridColor;
		public double gridThickness;
		public double axisThickness;
		#endregion

		#region Constructor/Init
		public PlotCanvas()
		{
			var bk = Widget.GetWidgetBackend(this);
			bk.EnableEvent(WidgetEvent.KeyPressed);
			bk.EnableEvent(WidgetEvent.KeyReleased);

			tickLabelFont = new TextLayout(this);

			

			LoadDefaultSettings();

			var g = new List<Function>();
			g.Add(Function.Parse("-(x^^2)+1", "f"));
			g.Add(Function.Parse("(x^^3) - sin(x)", "g"));
			//g.Add(Function.Parse("-sin(x)", "g"));
			graphs = g.ToArray();
			
		}

		void LoadDefaultSettings(bool redraw = false)
		{
			BaseLocation = new Point(0, 0);

			Scale_X = 4;
			Scale_Y = 4;

			MovingDelta = 1;
			ScalingDelta = 0.1;

			CalculationDensity = 2;

			TickDensity_XAxis = 0.2;
			TickDensity_YAxis = 0.2;

			axisColor = Colors.Black;
			axisThickness = 1;

			gridColor = Color.FromBytes(0xee, 0xee, 0xee);
			gridThickness = 1;

			tickLabelFont.Font = tickLabelFont.Font.WithPointSize(7);
		}
		#endregion

		#region Drawing

		public void Redraw()
		{
			clearBackground = true;
			QueueDraw();
		}

		public Image DrawToBitmap(bool fillBackground = false, int width = -1, int height = -1)
		{
			using (var imgBuilder = new Xwt.Drawing.ImageBuilder(
				Math.Max(width, (int)Size.Width),
				Math.Max(height, (int)Size.Height),
				ImageFormat.ARGB32))
			{
				clearBackground = fillBackground;
				// Backup, set and reset calculation density to 1 to achieve maximum render performance
				var density_Backup = CalculationDensity;
				CalculationDensity = 1;

				OnDraw(imgBuilder.Context, Bounds);

				CalculationDensity = density_Backup;

				return imgBuilder.ToImage();
			}
		}

		public void RenderIntoPng(string targetFile)
		{
			using (var img = DrawToBitmap())
				img.Save(targetFile, ImageFileType.Png);			
		}

		protected override void OnDraw(Context ctxt, Rectangle dirtyRect)
		{
			if (clearBackground)
			{
				clearBackground = false;
				ctxt.SetColor(Colors.White);
				ctxt.Rectangle(dirtyRect);
				ctxt.Fill();
				ctxt.Stroke();
			}
			ctxt.Save();

			DrawGrid(ctxt, dirtyRect);
			
			DrawGraphs(ctxt, dirtyRect);
			
			ctxt.Restore();
			
			base.OnDraw(ctxt, dirtyRect);
		}

		void DrawGrid(Context ctxt, Rectangle dirtyRect)
		{
			ctxt.SetColor(gridColor);
			if(gridThickness > 0)
				ctxt.SetLineWidth(gridThickness);

			// Draw vertical grid
			var tickDens_X = Scale_X * DotsPerCentimeter * TickDensity_XAxis;
			var initialVisPosition_X = -(BaseLocation.X % TickDensity_XAxis) * Scale_X * DotsPerCentimeter;

			if (gridThickness > 0)
				for (var visualPosition = initialVisPosition_X; 
					visualPosition < dirtyRect.Width; 
					visualPosition += tickDens_X)
				{
					ctxt.MoveTo(visualPosition, 0);
					ctxt.LineTo(visualPosition, dirtyRect.Height);
				}

			// Draw horizontal grid
			var tickDens_Y = Scale_Y * DotsPerCentimeter * TickDensity_YAxis;
			var initialVisPosition_Y = (BaseLocation.Y % TickDensity_YAxis) * Scale_Y * DotsPerCentimeter;
			if (gridThickness > 0)
				for (var visualPosition = initialVisPosition_Y; 
					visualPosition < dirtyRect.Height; 
					visualPosition += tickDens_Y)
				{
					ctxt.MoveTo(0,visualPosition);
					ctxt.LineTo(dirtyRect.Width, visualPosition);
				}

			ctxt.Stroke();

			if (axisThickness < 0)
				return;

			ctxt.SetColor(axisColor);
			ctxt.SetLineWidth(axisThickness);

			// Draw Y-Axis
			var axisPosition_y = -(BaseLocation.X * Scale_X * DotsPerCentimeter);
			if (axisPosition_y >= 0 && axisPosition_y <= dirtyRect.Width)
			{
				ctxt.Translate(axisPosition_y, 0);
				ctxt.MoveTo(0, 0);
				ctxt.LineTo(0, dirtyRect.Height);

				ctxt.Stroke();

				// Draw Y-Labels
				ctxt.MoveTo(0, 0);
				var y = Math.Round(BaseLocation.Y - (BaseLocation.Y % TickDensity_YAxis) - TickDensity_YAxis,1);
				for (var visualPosition = initialVisPosition_Y - tickDens_Y;
						visualPosition < dirtyRect.Height;
						visualPosition += tickDens_Y)
				{
					if (y < -0.01 || y > 0.01)
					{
						tickLabelFont.Text = Math.Round(y, 1).ToString();
						ctxt.DrawTextLayout(tickLabelFont, 2, visualPosition);
					}
					y -= TickDensity_YAxis;
				}

				ctxt.Stroke();
				ctxt.Translate(-axisPosition_y, 0);
			}	

			// Draw X-Axis
			var axisPosition_x = BaseLocation.Y * Scale_Y * DotsPerCentimeter;
			if (axisPosition_x >= 0 && axisPosition_x <= dirtyRect.Height)
			{
				ctxt.Translate(0, axisPosition_x);
				ctxt.MoveTo(0, 0);
				ctxt.LineTo(dirtyRect.Width, 0);

				ctxt.Stroke();

				// Draw X-Labels
				var labelYOffset = -tickLabelFont.Height - 2;
				ctxt.MoveTo(0, 0);
				var x = Math.Round(BaseLocation.X - (BaseLocation.X % TickDensity_XAxis) - TickDensity_XAxis, 1);
				for (var visualPosition = initialVisPosition_X - tickDens_X;
						visualPosition < dirtyRect.Width;
						visualPosition += tickDens_X)
				{
					if (x < -0.01 || x > 0.01)
					{
						tickLabelFont.Text = Math.Round(x,1).ToString();
						ctxt.DrawTextLayout(tickLabelFont, visualPosition, labelYOffset);
					}
					x += TickDensity_XAxis;
				}

				ctxt.Stroke();
				ctxt.Translate(0, -axisPosition_x);
			}

			ctxt.SetLineWidth(1);
		}

		double GetMinimalX()
		{
			return BaseLocation.X;
		}

		double GetMinimalY(double windowHeight)
		{
			return BaseLocation.Y - (windowHeight / Scale_Y);
		}

		double GetMaximalX(double windowWidth)
		{
			return BaseLocation.X + (windowWidth / (Scale_X * CalculationDensity));
		}

		double GetMaximalY()
		{
			return BaseLocation.Y;
		}

		void DrawGraphs(Context ctxt, Rectangle dirtyRect)
		{
			if (graphs == null || graphs.Length == 0)
				return;

			var x_max = GetMaximalX(dirtyRect.Width);
			var x_delta = DeltaX;

			var y_multiplier = -YMultiplier;
			var y = 0d;
			var y_min = GetMinimalY(dirtyRect.Height);
			var y_max = GetMaximalY();

			foreach (var f in graphs)
			{
				y = 0d;
				var x = GetMinimalX();
				ctxt.SetColor(f.GraphColor);
				ctxt.MoveTo(0, 0);
				for (var px = 0 ; px <= dirtyRect.Width; px += CalculationDensity)
				{
					try
					{
						y = f.Calculate(x);
						x += x_delta;

						if (y < y_min)
						{
							y = y_min;
						}
						else if (y > y_max)
							y = y_max;
						else
						{
							ctxt.LineTo(px, (y- y_max) * y_multiplier);
							continue;
						}
					}
					catch (DivideByZeroException ex)
					{
						x += x_delta;
					}

					ctxt.MoveTo(px, (y - y_max) * y_multiplier);
				}

				ctxt.Stroke();
			}
		}
		#endregion

		#region Scaling
		bool scaleOnScroll = false;
		bool clearBackground = false;
		bool moving;
		Point triggerPos;

		protected override void OnButtonPressed(ButtonEventArgs args)
		{
			if (args.Button == PointerButton.Left)
			{
				if (!moving)
					CalculationDensity *= 2;
				moving = true;
				triggerPos = new Point(args.X, args.Y);
			}
			base.OnButtonPressed(args);
		}

		protected override void OnButtonReleased(ButtonEventArgs args)
		{
			if (args.Button == PointerButton.Left)
			{
				if (moving)
					CalculationDensity /= 2;
				moving = false;
			}

			base.OnButtonReleased(args);
		}

		protected override void OnMouseMoved(MouseMovedEventArgs args)
		{
			base.OnMouseMoved(args);

			if (moving)
			{
				var newX = BaseLocation.X + (triggerPos.X - args.X) / (DotsPerCentimeter * Scale_X);
				var newY = BaseLocation.Y + (args.Y - triggerPos.Y) / (DotsPerCentimeter * Scale_Y);
				BaseLocation = new Point(newX, newY);
				triggerPos = new Point(args.X, args.Y);

				Redraw();
			}
		}

		protected override void OnMouseScrolled(MouseScrolledEventArgs args)
		{
			switch (args.Direction)
			{
				case ScrollDirection.Down:
					if (scaleOnScroll)
					{
						Scale_X -= ScalingDelta;
						Scale_Y -= ScalingDelta;
					}
					else
						BaseLocation.Y -= MovingDelta * Scale_Y;
					break;
				case ScrollDirection.Up:
					if (scaleOnScroll)
					{
						Scale_X += ScalingDelta;
						Scale_Y += ScalingDelta;
					}
					else
						BaseLocation.Y += MovingDelta * Scale_Y;
					break;
				case ScrollDirection.Left:
					BaseLocation.X -= MovingDelta * Scale_X;
					break;
				case ScrollDirection.Right:
					BaseLocation.X += MovingDelta * Scale_X;
					break;
			}

			Redraw();

			base.OnMouseScrolled(args);
		}

		protected override void OnKeyPressed(KeyEventArgs args)
		{
			base.OnKeyPressed(args);

			switch (args.Key)
			{
				case Key.ControlLeft:
				case Key.ControlRight:
					scaleOnScroll = true;
					return;
				case Key.Left:
					BaseLocation.X -= MovingDelta * Scale_X;
					break;
				case Key.Right:
					BaseLocation.X += MovingDelta * Scale_X;
					break;
				case Key.Up:
					BaseLocation.Y -= MovingDelta * Scale_Y;
					break;
				case Key.Down:
					BaseLocation.Y += MovingDelta * Scale_Y;
					break;
			}

			Redraw();
		}

		protected override void OnKeyReleased(KeyEventArgs args)
		{
			if (args.Key == Key.ControlLeft || args.Key == Key.ControlRight)
				scaleOnScroll = false;
			base.OnKeyReleased(args);
		}
		#endregion
	}
}
