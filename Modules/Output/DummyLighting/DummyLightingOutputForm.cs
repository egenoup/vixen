﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Vixen.Sys;
using Vixen.Commands;
using Vixen.Commands.KnownDataTypes;

namespace VixenModules.Output.DummyLighting
{
	public partial class DummyLightingOutputForm : Form
	{
		private byte[] _values = new byte[0];
		private Color[] _colorValues = new Color[0];
		private int _across = 1, _down = 1;
		private int _count;
		private int _boxWidth, _boxHeight;
		private SolidBrush _brush;
		private int _outputCount;
		private RenderStyle _renderingStyle;

		public DummyLightingOutputForm()
		{
			InitializeComponent();
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
			_brush = new SolidBrush(System.Drawing.Color.Black);

			// If the form has been created but not shown, the handle may not yet be
			// created.  If that's the case, then calls to InvokeRequired may return false
			// when it should really return true.
			IntPtr handle = this.Handle;

			renderingStyle = RenderStyle.Monochrome;
		}

		public RenderStyle renderingStyle
		{
			get { return _renderingStyle; }
			set { _renderingStyle = value; _generateValues(OutputCount); }
		}

		public int OutputCount
		{
			get { return _outputCount; }
			set
			{
				_outputCount = value;
				_generateValues(value);
			}
		}

		private void _generateValues(int outputs)
		{
			switch (renderingStyle) {
				case RenderStyle.Monochrome:
					_values = new byte[outputs];
					_colorValues = null;
					break;

				case RenderStyle.RGBMultiChannel:
					_values = new byte[outputs];
					_colorValues = null;
					// pretend we only have 1/3 the outputs we were told for calculations below, as 3 outputs == 1 square
					outputs = (int)Math.Ceiling(outputs / 3.0);
					break;

				case RenderStyle.RGBSingleChannel:
					_values = null;
					_colorValues = new Color[outputs];
					break;
			}

			if (outputs > 0) {
				_across = (int)Math.Sqrt(outputs);
				_down = (int)Math.Round((double)outputs / _across, MidpointRounding.AwayFromZero);
				_boxWidth = (ClientRectangle.Width / _across) - 1;
				_boxHeight = (ClientRectangle.Height / _down) - 1;
			} else {
				_across = 0;
				_down = 0;
				_boxWidth = 0;
				_boxHeight = 0;
			}

		}

		new public void Show()
		{
			// Reset state.
			_count = 0;
			if (_values != null)
				Array.Clear(_values, 0, _values.Length);
			if (_colorValues != null)
				Array.Clear(_colorValues, 0, _colorValues.Length);

			BeginInvoke(new MethodInvoker(base.Show));
		}

		new public void Hide()
		{
		    if (!this.IsDisposed)
		    {
		        BeginInvoke(new MethodInvoker(base.Hide));
		    }
		}

		private double _fps;
		public void UpdateState(double fps, Command[] outputStates)
		{
			_count++;
			_fps = fps;

			Command command;
			for (int i = 0; i < outputStates.Length; i++) {
				command = outputStates[i];
				if (renderingStyle == RenderStyle.Monochrome || renderingStyle == RenderStyle.RGBMultiChannel) {
					if (command != null) {
						if (command is Lighting.Monochrome.SetLevel) {
							Level level = (command as Lighting.Monochrome.SetLevel).Level;
							_values[i] = (byte)(level * byte.MaxValue / 100);
						}
					} else {
						// Clear the output.
						_values[i] = 0;
					}
				} else if (renderingStyle == RenderStyle.RGBSingleChannel) {
					if (command != null) {
						if (command is Lighting.Polychrome.SetColor) {
							_colorValues[i] = (command as Lighting.Polychrome.SetColor).Color;
						}
					} else {
						_colorValues[i] = Color.Black;
					}
				}
			}

		    if (!IsDisposed)
		    {
		        BeginInvoke(new MethodInvoker(Refresh));
		    }
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			//Not taking any optimizing steps to see how it does.
			System.Drawing.Color color;
			e.Graphics.Clear(System.Drawing.Color.Black);

			switch (renderingStyle) {
				case RenderStyle.Monochrome:
					for (int i = 0; i < _values.Length; i++) {
						color = System.Drawing.Color.FromArgb(_values[i], System.Drawing.Color.White);
						_brush.Color = color;
						e.Graphics.FillRectangle(_brush, (i % _across) * (_boxWidth + 1), (i / _across) * (_boxHeight + 1), _boxWidth, _boxHeight);
					}
					break;

				case RenderStyle.RGBMultiChannel:
					for (int i = 2; i < _values.Length; i += 3) {
						byte R = _values[i - 2];
						byte G = _values[i - 1];
						byte B = _values[i - 0];

						color = System.Drawing.Color.FromArgb(R, G, B);
						_brush.Color = color;

						int square = i / 3;
						e.Graphics.FillRectangle(_brush, (square % _across) * (_boxWidth + 1), (square / _across) * (_boxHeight + 1), _boxWidth, _boxHeight);
					}
					break;

				case RenderStyle.RGBSingleChannel:
					for (int i = 0; i < _colorValues.Length; i++) {
						color = _colorValues[i];
						_brush.Color = color;
						e.Graphics.FillRectangle(_brush, (i % _across) * (_boxWidth + 1), (i / _across) * (_boxHeight + 1), _boxWidth, _boxHeight);
					}
					break;
			}

			e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			e.Graphics.DrawString(string.Format("{0:F2} fps", _fps), Font, Brushes.Red, 2, 2);
		}
	}

	public enum RenderStyle
	{
		// each output is a single monochrome output
		Monochrome,

		// every 3 outputs is a single RGB output
		RGBMultiChannel,

		// every output is a single RGB output (and needs 'SetColor' commands)
		RGBSingleChannel
	}
}

