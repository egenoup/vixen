﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Vixen.Execution.Context;
using Vixen.Module.Preview;
using Vixen.Data.Value;
using Vixen.Sys;
using System.IO;
using VixenModules.Preview.VixenPreview.Shapes;
using VixenModules.Property.Location;

namespace VixenModules.Preview.VixenPreview {
	public partial class VixenPreviewSetup3 : Form {
		private VixenPreviewData _data;
		private VixenPreviewSetupDocument previewForm;
		private VixenPreviewSetupElementsDocument elementsForm;
		private VixenPreviewSetupPropertiesDocument propertiesForm;
		private static NLog.Logger Logging = NLog.LogManager.GetCurrentClassLogger();

		public VixenPreviewData Data {
			set {
				_data = value;
				if (!DesignMode && previewForm != null)
					previewForm.Preview.Data = _data;
			}
			get
			{
				if (_data == null) {
					Logging.Warn("VixenPreviewSetup3: access of null Data. (Thread ID: " +
					                            System.Threading.Thread.CurrentThread.ManagedThreadId + ")");
				}
				return _data;
			}
		}

		public VixenPreviewSetup3() {
			InitializeComponent();
		}

		private void VixenPreviewSetup3_Load(object sender, EventArgs e) {
			previewForm = new VixenPreviewSetupDocument();
			if (!DesignMode && previewForm != null)
				previewForm.Preview.Data = _data;
			previewForm.Preview.OnSelectDisplayItem += OnSelectDisplayItem;
			previewForm.Preview.OnDeSelectDisplayItem += OnDeSelectDisplayItem;

			elementsForm = new VixenPreviewSetupElementsDocument(previewForm.Preview);
			propertiesForm = new VixenPreviewSetupPropertiesDocument();
			previewForm.Show(dockPanel);
			elementsForm.Show(dockPanel, WeifenLuo.WinFormsUI.Docking.DockState.DockLeft);
			propertiesForm.Show(elementsForm.Pane, WeifenLuo.WinFormsUI.Docking.DockAlignment.Bottom, 0.5);

			previewForm.Preview.elementsForm = elementsForm;
			previewForm.Preview.propertiesForm = propertiesForm;

			previewForm.Preview.LoadBackground(Data.BackgroundFileName);
			trackBarBackgroundAlpha.Value = Data.BackgroundAlpha;
			previewForm.Preview.Reload();

			PopulateTemplateList();

			Setup();

			// disable the D2D preview option for now; the GDI performs just as well, and is more reliable (eg. older machines, not HW accelerated machines, etc.)
			//performanceToolStripMenuItem.Visible = Vixen.Sys.VixenSystem.VersionBeyondWindowsXP;
			performanceToolStripMenuItem.Visible = false;

			Properties.Settings settings = new Properties.Settings();

			useDirect2DPreviewRenderingToolStripMenuItem.Checked = !settings.UseGDIRendering;
			saveLocationsToolStripMenuItem.Checked = Data.SaveLocations;
		}

		private void buttonSetBackground_Click(object sender, EventArgs e) {
			if (dialogSelectBackground.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
				// Copy the file to the Vixen folder
				var imageFile = new System.IO.FileInfo(dialogSelectBackground.FileName);
				var destFileName = Path.Combine(VixenPreviewDescriptor.ModulePath, imageFile.Name);
				var sourceFileName = imageFile.FullName;
				if (sourceFileName != destFileName) {
					File.Copy(sourceFileName, destFileName, true);
				}

				// Set the backgrounds
				Data.BackgroundFileName = destFileName;
				previewForm.Preview.LoadBackground(dialogSelectBackground.FileName);
				trackBarBackgroundAlpha.Value = trackBarBackgroundAlpha.Maximum;
				previewForm.Preview.BackgroundAlpha = trackBarBackgroundAlpha.Value;
			}
		}

		private void OnDeSelectDisplayItem(object sender, Shapes.DisplayItem displayItem) {
			propertiesForm.ShowSetupControl(null);
		}

		private void OnSelectDisplayItem(object sender, Shapes.DisplayItem displayItem) {
			Shapes.DisplayItemBaseControl setupControl = displayItem.Shape.GetSetupControl();

			if (setupControl != null) {
				propertiesForm.ShowSetupControl(setupControl);
			}
		}

		private void toolbarButton_Click(object sender, EventArgs e) {
			Button button = sender as Button;
			// Select Button
			if (button == buttonSelect)
				previewForm.Preview.CurrentTool = VixenPreviewControl.Tools.Select;
			// Standard Buttons
			else if (button == buttonDrawPixel)
				previewForm.Preview.CurrentTool = VixenPreviewControl.Tools.Single;
			else if (button == buttonLine)
				previewForm.Preview.CurrentTool = VixenPreviewControl.Tools.String;
			else if (button == buttonSemiCircle)
				previewForm.Preview.CurrentTool = VixenPreviewControl.Tools.Arch;
			else if (button == buttonRectangle)
				previewForm.Preview.CurrentTool = VixenPreviewControl.Tools.Rectangle;
			else if (button == buttonEllipse)
				previewForm.Preview.CurrentTool = VixenPreviewControl.Tools.Ellipse;
			else if (button == buttonTriangle)
				previewForm.Preview.CurrentTool = VixenPreviewControl.Tools.Triangle;
			else if (button == buttonNet)
				previewForm.Preview.CurrentTool = VixenPreviewControl.Tools.Net;
			//else if (button == buttonFlood)
			//    previewForm.Preview.CurrentTool = VixenPreviewControl.Tools.Flood;
			else if (button == buttonCane)
				previewForm.Preview.CurrentTool = VixenPreviewControl.Tools.Cane;
			else if (button == buttonStar)
				previewForm.Preview.CurrentTool = VixenPreviewControl.Tools.Star;
			else if (button == buttonHelp)
				Common.VixenHelp.VixenHelp.ShowHelp(Common.VixenHelp.VixenHelp.HelpStrings.Preview_Main);
			else if (button == buttonMegaTree)
				previewForm.Preview.CurrentTool = VixenPreviewControl.Tools.MegaTree;
			else if (button == buttonPixelGrid)
				previewForm.Preview.CurrentTool = VixenPreviewControl.Tools.PixelGrid;
		}

		private void trackBarBackgroundAlpha_ValueChanged(object sender, EventArgs e) {
			previewForm.Preview.BackgroundAlpha = trackBarBackgroundAlpha.Value;
		}

		public void Setup() {
			SetDesktopLocation(Data.SetupLeft, Data.SetupTop);
			Size = new Size(Data.SetupWidth, Data.SetupHeight);
		}

		private void buttonSave_Click(object sender, EventArgs e) {
			SaveLocationDataForElements();
			DialogResult = System.Windows.Forms.DialogResult.OK;
			previewForm.Close();
			Close();
		}

		private void VixenPreviewSetup3_Move(object sender, EventArgs e) {
			if (Data == null) {
				Logging.Warn("VixenPreviewSetup3_Move: Data is null. abandoning move. (Thread ID: " +
											System.Threading.Thread.CurrentThread.ManagedThreadId + ")");
				return;
			}

			Data.SetupTop = Top;
			Data.SetupLeft = Left;
		}

		private void VixenPreviewSetup3_Resize(object sender, EventArgs e) {
			if (Data == null) {
				Logging.Warn("VixenPreviewSetup3_Resize: Data is null. abandoning resize. (Thread ID: " +
											System.Threading.Thread.CurrentThread.ManagedThreadId + ")");
				return;
			}

			Data.SetupWidth = Width;
			Data.SetupHeight = Height;
		}

		private void copyToolStripMenuItem_Click(object sender, EventArgs e) {
			previewForm.Preview.Cut();
		}

		private void copyToolStripMenuItem1_Click(object sender, EventArgs e) {
			previewForm.Preview.Copy();
		}

		private void pasteToolStripMenuItem_Click(object sender, EventArgs e) {
			previewForm.Preview.Paste();
		}

		private void deleteToolStripMenuItem_Click(object sender, EventArgs e) {
			previewForm.Preview.Delete();
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
			Close();
		}

		private void backgroundPropertiesToolStripMenuItem_Click(object sender, EventArgs e) {
			ResizePreviewForm resizeForm = new ResizePreviewForm(previewForm.Preview.Background.Width,
																 previewForm.Preview.Background.Height);
			if (resizeForm.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
				previewForm.Preview.ResizeBackground(resizeForm.Width, resizeForm.Height);
			}
		}

		#region Templates

		private void PopulateTemplateList() {
			TemplateComboBoxItem selectedTemplateItem = comboBoxTemplates.SelectedItem as TemplateComboBoxItem;
			comboBoxTemplates.Items.Clear();

			IEnumerable<string> files = System.IO.Directory.EnumerateFiles(PreviewTools.TemplateFolder, "*.xml");
			foreach (string file in files) {
				string fileName = PreviewTools.TemplateWithFolder(file);
				try {
					// Read the entire template file (stoopid waste of resources, but how else?)
					string xml = System.IO.File.ReadAllText(fileName);
					DisplayItem newDisplayItem = (DisplayItem)PreviewTools.DeSerializeToObject(xml, typeof(DisplayItem));
					TemplateComboBoxItem newTemplateItem = new TemplateComboBoxItem(newDisplayItem.Shape.Name, fileName);
					comboBoxTemplates.Items.Add(newTemplateItem);
				}
				catch (Exception ex) {
					MessageBox.Show("There was an error loading the template file (" + file + "): " + ex.Message,
									"Error Loading Template", MessageBoxButtons.OKCancel);
				}
				finally {
					if (selectedTemplateItem != null && comboBoxTemplates.Items.IndexOf(selectedTemplateItem) >= 0) {
						comboBoxTemplates.SelectedItem = selectedTemplateItem;
					}
					if (comboBoxTemplates.SelectedItem == null && comboBoxTemplates.Items.Count > 0) {
						comboBoxTemplates.SelectedIndex = 0;
					}
				}
			}
		}

		private void buttonAddTemplate_Click(object sender, EventArgs e) {
			previewForm.Preview.CreateTemplate();
			PopulateTemplateList();
		}

		private void buttonAddToPreview_Click(object sender, EventArgs e) {
			TemplateComboBoxItem templateItem = comboBoxTemplates.SelectedItem as TemplateComboBoxItem;
			if (templateItem != null) {
				previewForm.Preview.AddTtemplateToPreview(templateItem.FileName);
			}
		}

		private void buttonDeleteTemplate_Click(object sender, EventArgs e) {
			TemplateComboBoxItem templateItem = comboBoxTemplates.SelectedItem as TemplateComboBoxItem;
			if (templateItem != null) {
				if (System.IO.File.Exists(templateItem.FileName)) {
					if (
						MessageBox.Show("Are you sure you want to delete the template '" + templateItem.FileName + "'", "Delete Template",
										MessageBoxButtons.YesNoCancel) == System.Windows.Forms.DialogResult.Yes) {
						System.IO.File.Delete(templateItem.FileName);
						PopulateTemplateList();
					}
				}
			}
		}

		private void buttonTemplateHelp_Click(object sender, EventArgs e) {
			Common.VixenHelp.VixenHelp.ShowHelp(Common.VixenHelp.VixenHelp.HelpStrings.Preview_CustomShape);
		}

		#endregion // Templates

		private void buttonSelect_Click(object sender, EventArgs e) {
			previewForm.Preview.CurrentTool = VixenPreviewControl.Tools.Select;
		}

		private void SaveLocationDataForElements()
		{
			if (Data.SaveLocations)
			{
				Cursor = Cursors.WaitCursor;
				foreach (var d in _data.DisplayItems)
				{
					//_data.DisplayItems.ForEach(d => {
					foreach (var p in d.Shape.Pixels.Where(pi => pi != null && pi.Node != null))
					{

						//LocationModule prop= null;
						if (!p.Node.Properties.Contains(LocationDescriptor._typeId))
							p.Node.Properties.Add(LocationDescriptor._typeId);

						//d.Shape._pixels.ForEach(p => {

						var prop = p.Node.Properties.Get(LocationDescriptor._typeId);
						((LocationData) prop.ModuleData).X = p.X;
						((LocationData) prop.ModuleData).Y = p.Y;

						//});
						//});
					}
				}
				Cursor = Cursors.Default;
			}
		}

		private void propInformationToolStripMenuItem_Click(object sender, EventArgs e)
		{
			previewForm.Preview.ShowInfo = !previewForm.Preview.ShowInfo;
			propInformationToolStripMenuItem.Checked = previewForm.Preview.ShowInfo;
		}

		private void useDirect2DPreviewRenderingToolStripMenuItem_Click(object sender, EventArgs e) {
			var msg = MessageBox.Show("Preview will be restarted. This is a system-wide change that will apply to all previews. Are you sure you want to do this?", "Change Preview", MessageBoxButtons.YesNo);
			if (msg == System.Windows.Forms.DialogResult.Yes) {
				Properties.Settings settings = new Properties.Settings();
				settings.UseGDIRendering = !useDirect2DPreviewRenderingToolStripMenuItem.Checked;
				settings.Save();
				

			}
		}

		private void saveLocationsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Data.SaveLocations = saveLocationsToolStripMenuItem.Checked;
		}
	}
}