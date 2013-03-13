using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml;
using Xwt;
using Xwt.Backends;
using Xwt.Drawing;

namespace GraphPlotter.Plotting
{
	class PlotCanvas : Canvas
	{
		#region Properties
		readonly HBox funcOverlayBox;
		readonly SettingsOverlay settingsOverlay;
		readonly FunctionEditingOverlay funcOverlay;
		bool updatingGraphEntries;

		public readonly PlotCanvasOptions Options;

		public bool SettingsOverlayVisible { 
			get { return settingsOverlay.Visible; }
			set { settingsOverlay.Visible = value; }
		}

		public bool FunctionOverlayVisible
		{
			get { return funcOverlay.Visible; }
			set { funcOverlay.Visible = value; }
		}
		#endregion

		#region Constructor/Init
		public PlotCanvas()
		{
			Options = new PlotCanvasOptions(this);

			CanGetFocus = true;

			funcOverlayBox = new HBox();
			AddChild(funcOverlayBox);

			funcOverlayBox.PackStart(settingsOverlay = new SettingsOverlay(this) { MarginLeft = 2, MarginRight = -10 }, BoxMode.None);
			funcOverlayBox.PackEnd(funcOverlay = new FunctionEditingOverlay(this), BoxMode.Fill);

			Options.Functions.CollectionChanged += Graphs_CollectionChanged;
			


			BeginUpdateGraphs();
			Options.Functions.Add(Function.Parse("f", "-(x^^2)-sin(x*pi*8)"));
			Options.Functions.Add(Function.Parse("g", "sin(x*pi*2)"));
			FinishUpdateGraphs();
		}

		void Graphs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
				foreach(Function f in e.NewItems)
					f.PropertyChanged += f_PropertyChanged;

			if (!updatingGraphEntries)
				Redraw();
		}

		void f_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (!updatingGraphEntries && (e.PropertyName == "Visible" || e.PropertyName == "CompiledExpression"))
				Redraw();
		}
		#endregion

		#region Settings
		public void BeginUpdateGraphs() { updatingGraphEntries = true; }
		public void FinishUpdateGraphs() { updatingGraphEntries = false; Redraw(); }
		#endregion

		#region Drawing (Highlevel)
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
				var density_Backup = Options.CalculationDensity;
				Options.CalculationDensity = 1;

				OnDraw(imgBuilder.Context, Bounds);

				Options.CalculationDensity = density_Backup;

				return imgBuilder.ToImage();
			}
		}

		public void RenderIntoPng(string targetFile)
		{
			using (var img = DrawToBitmap())
				img.Save(targetFile, ImageFileType.Png);
		}
		#endregion

		#region Drawing
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
			var Scale_X = Options.Scale_X;
			var Scale_Y = Options.Scale_Y;
			var TickDensity_XAxis = Options.TickDensity_XAxis;
			var TickDensity_YAxis = Options.TickDensity_YAxis;
			var Base_X = Options.BaseLocation.X;
			var Base_Y = Options.BaseLocation.Y;

			ctxt.SetColor(Options.gridColor);
			if (Options.gridThickness > 0)
				ctxt.SetLineWidth(Options.gridThickness);

			// Draw vertical grid
			var tickDens_X = Scale_X * PlotCanvasOptions.DotsPerCentimeter * TickDensity_XAxis;
			var initialVisPosition_X = -(Base_X % TickDensity_XAxis) * Scale_X * PlotCanvasOptions.DotsPerCentimeter;

			if (Options.gridThickness > 0)
				for (var visualPosition = initialVisPosition_X; 
					visualPosition < dirtyRect.Width; 
					visualPosition += tickDens_X)
				{
					ctxt.MoveTo(visualPosition, 0);
					ctxt.LineTo(visualPosition, dirtyRect.Height);
				}

			// Draw horizontal grid
			var tickDens_Y = Scale_Y * PlotCanvasOptions.DotsPerCentimeter * TickDensity_YAxis;
			var initialVisPosition_Y = (Base_Y % TickDensity_YAxis) * Scale_Y * PlotCanvasOptions.DotsPerCentimeter;
			if (Options.gridThickness > 0)
				for (var visualPosition = initialVisPosition_Y; 
					visualPosition < dirtyRect.Height; 
					visualPosition += tickDens_Y)
				{
					ctxt.MoveTo(0,visualPosition);
					ctxt.LineTo(dirtyRect.Width, visualPosition);
				}

			ctxt.Stroke();

			if (Options.axisThickness < 0)
				return;

			ctxt.SetColor(Options.axisColor);
			ctxt.SetLineWidth(Options.axisThickness);

			// Draw Y-Axis
			var axisPosition_y = -(Base_X * Scale_X * PlotCanvasOptions.DotsPerCentimeter);
			if (axisPosition_y >= 0 && axisPosition_y <= dirtyRect.Width)
			{
				ctxt.Translate(axisPosition_y, 0);
				ctxt.MoveTo(0, 0);
				ctxt.LineTo(0, dirtyRect.Height);

				ctxt.Stroke();

				// Draw Y-Labels
				ctxt.MoveTo(0, 0);
				var y = Math.Round(Base_Y - (Base_Y % TickDensity_YAxis) + TickDensity_YAxis,1);
				for (var visualPosition = initialVisPosition_Y - tickDens_Y;
						visualPosition < dirtyRect.Height;
						visualPosition += tickDens_Y)
				{
					if (y < -0.01 || y > 0.01)
					{
						Options.tickLabelFont.Text = Math.Round(y, 1).ToString();
						ctxt.DrawTextLayout(Options.tickLabelFont, 2, visualPosition);
					}
					y -= TickDensity_YAxis;
				}

				ctxt.Stroke();
				ctxt.Translate(-axisPosition_y, 0);
			}	

			// Draw X-Axis
			var axisPosition_x = Base_Y * Scale_Y * PlotCanvasOptions.DotsPerCentimeter;
			if (axisPosition_x >= 0 && axisPosition_x <= dirtyRect.Height)
			{
				ctxt.Translate(0, axisPosition_x);
				ctxt.MoveTo(0, 0);
				ctxt.LineTo(dirtyRect.Width, 0);

				ctxt.Stroke();

				// Draw X-Labels
				var labelYOffset = -Options.tickLabelFont.Height - 2;
				ctxt.MoveTo(0, 0);
				var x = Math.Round(Base_X - (Base_X % TickDensity_XAxis) - TickDensity_XAxis, 1);
				for (var visualPosition = initialVisPosition_X - tickDens_X;
						visualPosition < dirtyRect.Width;
						visualPosition += tickDens_X)
				{
					if (x < -0.01 || x > 0.01)
					{
						Options.tickLabelFont.Text = Math.Round(x,1).ToString();
						ctxt.DrawTextLayout(Options.tickLabelFont, visualPosition, labelYOffset);
					}
					x += TickDensity_XAxis;
				}

				ctxt.Stroke();
				ctxt.Translate(0, -axisPosition_x);
			}

			ctxt.SetLineWidth(1);
		}

		void DrawGraphs(Context ctxt, Rectangle dirtyRect)
		{
			if (Options.Functions.Count == 0)
				return;

			var calcDens = Options.CalculationDensity;

			var x_max = Options.BaseLocation.X + (dirtyRect.Width / (Options.Scale_X * calcDens));
			var x_delta = Options.DeltaX;

			var y_multiplier = -Options.YMultiplier;
			var y = 0d;
			var y_min = Options.BaseLocation.Y - (dirtyRect.Height / Options.Scale_Y);
			var y_max = Options.BaseLocation.Y;

			foreach (var f in Options.Functions)
			{
				if (!f.Visible)
					continue;

				y = 0d;
				var x = Options.BaseLocation.X;
				ctxt.SetColor(f.GraphColor);
				ctxt.MoveTo(0, 0);
				for (var px = 0; px <= dirtyRect.Width; px += calcDens)
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

		public void CenterBaseLocation(bool redraw = true)
		{
			var sz = Size;
			Options.BaseLocation_X = -(sz.Width / (2 * PlotCanvasOptions.DotsPerCentimeter * Options.Scale_X));
			Options.BaseLocation_Y = sz.Height / (2 * PlotCanvasOptions.DotsPerCentimeter * Options.Scale_Y);

			if (redraw)
				Redraw();
		}

		public void RestoreDefaultScaling(bool redraw = true)
		{
			Options.RestoreDefaultScaling();
			if (redraw)
				Redraw();
		}

		protected override void OnButtonPressed(ButtonEventArgs args)
		{
			if (args.Button == PointerButton.Left)
			{
				SetFocus();
				if (!moving)
					Options.CalculationDensity *= 2;
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
					Options.CalculationDensity /= 2;
				moving = false;
			}

			base.OnButtonReleased(args);
		}

		protected override void OnMouseMoved(MouseMovedEventArgs args)
		{
			base.OnMouseMoved(args);

			if (moving)
			{
				Options.BaseLocation_X = Options.BaseLocation_X + (triggerPos.X - args.X) / (PlotCanvasOptions.DotsPerCentimeter * Options.Scale_X);
				Options.BaseLocation_Y = Options.BaseLocation_Y + (args.Y - triggerPos.Y) / (PlotCanvasOptions.DotsPerCentimeter * Options.Scale_Y);
				triggerPos = new Point(args.X, args.Y);

				Redraw();
			}
		}

		protected override void OnMouseScrolled(MouseScrolledEventArgs args)
		{
			var ScalingDelta = Options.ScalingDelta;
			var MovingDelta = Options.MovingDelta;

			switch (args.Direction)
			{
				case ScrollDirection.Down:
					if (scaleOnScroll)
					{
						Options.Scale_X /= ScalingDelta;
						Options.Scale_Y /= ScalingDelta;
					}
					else
						Options.BaseLocation_Y -= MovingDelta / Options.Scale_Y;
					break;
				case ScrollDirection.Up:
					if (scaleOnScroll)
					{
						Options.Scale_X *= ScalingDelta;
						Options.Scale_Y *= ScalingDelta;
					}
					else
						Options.BaseLocation_Y += MovingDelta / Options.Scale_Y;
					break;
				case ScrollDirection.Left:
					Options.BaseLocation_X -= MovingDelta / Options.Scale_X;
					break;
				case ScrollDirection.Right:
					Options.BaseLocation_X += MovingDelta / Options.Scale_X;
					break;
			}

			Redraw();

			base.OnMouseScrolled(args);
		}

		protected override void OnKeyPressed(KeyEventArgs args)
		{
			var MovingDelta = Options.MovingDelta;
			base.OnKeyPressed(args);

			switch (args.Key)
			{
				case Key.ControlLeft:
				case Key.ControlRight:
					scaleOnScroll = true;
					return;
				case Key.Left:
					Options.BaseLocation_X -= MovingDelta / Options.Scale_X;
					break;
				case Key.Right:
					Options.BaseLocation_X += MovingDelta / Options.Scale_X;
					break;
				case Key.Up:
					Options.BaseLocation_Y += MovingDelta / Options.Scale_Y;
					break;
				case Key.Down:
					Options.BaseLocation_Y -= MovingDelta / Options.Scale_Y;
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

		protected override void OnGotFocus(EventArgs args)
		{
			base.OnGotFocus(args);
		}

		protected override void OnLostFocus(EventArgs args)
		{
			scaleOnScroll = false;
			moving = false;
			base.OnLostFocus(args);
		}
		#endregion

		#region Function editing overlays
		bool init = true;
		protected override void OnBoundsChanged()
		{
			base.OnBoundsChanged();
			if (init)
			{
				init = false;
				CenterBaseLocation(true);
			}

			var w = Size;
			SetChildBounds(funcOverlayBox,new Rectangle(0,0,w.Width, w.Height));
		}
		#endregion
	}
}
