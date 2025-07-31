/*
 * Copyright © 2017 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */
namespace ActionLanguage
{
    partial class ActionProgramEditForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ActionProgramEditForm));
            this.panelOuter = new System.Windows.Forms.Panel();
            this.extPanelVertScrollWithBar = new ExtendedControls.ExtPanelVertScrollWithBar();
            this.panelVScroll = new ExtendedControls.ExtPanelVertScroll();
            this.buttonMore = new ExtendedControls.ExtButton();
            this.panelName = new System.Windows.Forms.Panel();
            this.buttonExtDelete = new ExtendedControls.ExtButton();
            this.textBoxBorderName = new ExtendedControls.ExtTextBox();
            this.labelSet = new System.Windows.Forms.Label();
            this.labelName = new System.Windows.Forms.Label();
            this.panelTop = new System.Windows.Forms.Panel();
            this.panel_close = new ExtendedControls.ExtButtonDrawn();
            this.panel_minimize = new ExtendedControls.ExtButtonDrawn();
            this.label_index = new System.Windows.Forms.Label();
            this.panelOK = new System.Windows.Forms.Panel();
            this.extButtonHeader = new ExtendedControls.ExtButton();
            this.buttonExtDisk = new ExtendedControls.ExtButton();
            this.buttonExtLoad = new ExtendedControls.ExtButton();
            this.buttonExtSave = new ExtendedControls.ExtButton();
            this.buttonExtEdit = new ExtendedControls.ExtButton();
            this.buttonCancel = new ExtendedControls.ExtButton();
            this.buttonOK = new ExtendedControls.ExtButton();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.insertEntryAboveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.whitespaceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeWhitespaceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editCommentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.statusStripCustom = new ExtendedControls.ExtStatusStrip();
            this.panelOuter.SuspendLayout();
            this.extPanelVertScrollWithBar.SuspendLayout();
            this.panelVScroll.SuspendLayout();
            this.panelName.SuspendLayout();
            this.panelTop.SuspendLayout();
            this.panelOK.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelOuter
            // 
            this.panelOuter.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelOuter.Controls.Add(this.extPanelVertScrollWithBar);
            this.panelOuter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelOuter.Location = new System.Drawing.Point(3, 60);
            this.panelOuter.Name = "panelOuter";
            this.panelOuter.Padding = new System.Windows.Forms.Padding(3);
            this.panelOuter.Size = new System.Drawing.Size(862, 388);
            this.panelOuter.TabIndex = 9;
            // 
            // extPanelVertScrollWithBar
            // 
            this.extPanelVertScrollWithBar.Controls.Add(this.panelVScroll);
            this.extPanelVertScrollWithBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.extPanelVertScrollWithBar.HideScrollBar = false;
            this.extPanelVertScrollWithBar.LargeChange = 10;
            this.extPanelVertScrollWithBar.Location = new System.Drawing.Point(3, 3);
            this.extPanelVertScrollWithBar.Name = "extPanelVertScrollWithBar";
            this.extPanelVertScrollWithBar.ScrollValue = 0;
            this.extPanelVertScrollWithBar.Size = new System.Drawing.Size(854, 380);
            this.extPanelVertScrollWithBar.SmallChange = 1;
            this.extPanelVertScrollWithBar.TabIndex = 6;
            // 
            // panelVScroll
            // 
            this.panelVScroll.Controls.Add(this.buttonMore);
            this.panelVScroll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelVScroll.Location = new System.Drawing.Point(0, 0);
            this.panelVScroll.Name = "panelVScroll";
            this.panelVScroll.Size = new System.Drawing.Size(806, 380);
            this.panelVScroll.TabIndex = 8;
            this.panelVScroll.Value = 0;
            this.panelVScroll.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panelVScroll_MouseDown);
            this.panelVScroll.MouseMove += new System.Windows.Forms.MouseEventHandler(this.panelVScroll_MouseMove);
            this.panelVScroll.MouseUp += new System.Windows.Forms.MouseEventHandler(this.panelVScroll_MouseUp);
            this.panelVScroll.Resize += new System.EventHandler(this.panelVScroll_Resize);
            // 
            // buttonMore
            // 
            this.buttonMore.BackColor2 = System.Drawing.Color.Red;
            this.buttonMore.ButtonDisabledScaling = 0.5F;
            this.buttonMore.GradientDirection = 90F;
            this.buttonMore.Location = new System.Drawing.Point(6, 6);
            this.buttonMore.MouseOverScaling = 1.3F;
            this.buttonMore.MouseSelectedScaling = 1.3F;
            this.buttonMore.Name = "buttonMore";
            this.buttonMore.Size = new System.Drawing.Size(22, 22);
            this.buttonMore.TabIndex = 5;
            this.buttonMore.Text = "+";
            this.buttonMore.UseVisualStyleBackColor = true;
            this.buttonMore.Click += new System.EventHandler(this.buttonMore_Click);
            // 
            // panelName
            // 
            this.panelName.Controls.Add(this.buttonExtDelete);
            this.panelName.Controls.Add(this.textBoxBorderName);
            this.panelName.Controls.Add(this.labelSet);
            this.panelName.Controls.Add(this.labelName);
            this.panelName.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelName.Location = new System.Drawing.Point(3, 24);
            this.panelName.Name = "panelName";
            this.panelName.Size = new System.Drawing.Size(862, 36);
            this.panelName.TabIndex = 8;
            // 
            // buttonExtDelete
            // 
            this.buttonExtDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonExtDelete.BackColor2 = System.Drawing.Color.Red;
            this.buttonExtDelete.ButtonDisabledScaling = 0.5F;
            this.buttonExtDelete.GradientDirection = 90F;
            this.buttonExtDelete.Location = new System.Drawing.Point(833, 4);
            this.buttonExtDelete.MouseOverScaling = 1.3F;
            this.buttonExtDelete.MouseSelectedScaling = 1.3F;
            this.buttonExtDelete.Name = "buttonExtDelete";
            this.buttonExtDelete.Size = new System.Drawing.Size(25, 23);
            this.buttonExtDelete.TabIndex = 25;
            this.buttonExtDelete.Text = "X";
            this.buttonExtDelete.UseVisualStyleBackColor = true;
            this.buttonExtDelete.Click += new System.EventHandler(this.buttonExtDelete_Click);
            // 
            // textBoxBorderName
            // 
            this.textBoxBorderName.BackErrorColor = System.Drawing.Color.Red;
            this.textBoxBorderName.BorderColor = System.Drawing.Color.Transparent;
            this.textBoxBorderName.BorderColor2 = System.Drawing.Color.Transparent;
            this.textBoxBorderName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxBorderName.ClearOnFirstChar = false;
            this.textBoxBorderName.ControlBackground = System.Drawing.SystemColors.Control;
            this.textBoxBorderName.EndButtonEnable = true;
            this.textBoxBorderName.EndButtonImage = ((System.Drawing.Image)(resources.GetObject("textBoxBorderName.EndButtonImage")));
            this.textBoxBorderName.EndButtonSize16ths = 10;
            this.textBoxBorderName.EndButtonVisible = false;
            this.textBoxBorderName.InErrorCondition = false;
            this.textBoxBorderName.Location = new System.Drawing.Point(152, 4);
            this.textBoxBorderName.Multiline = false;
            this.textBoxBorderName.Name = "textBoxBorderName";
            this.textBoxBorderName.ReadOnly = false;
            this.textBoxBorderName.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.textBoxBorderName.SelectionLength = 0;
            this.textBoxBorderName.SelectionStart = 0;
            this.textBoxBorderName.Size = new System.Drawing.Size(154, 20);
            this.textBoxBorderName.TabIndex = 0;
            this.textBoxBorderName.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.textBoxBorderName.TextNoChange = "";
            this.textBoxBorderName.WordWrap = true;
            // 
            // labelSet
            // 
            this.labelSet.AutoSize = true;
            this.labelSet.Location = new System.Drawing.Point(53, 7);
            this.labelSet.Name = "labelSet";
            this.labelSet.Size = new System.Drawing.Size(43, 13);
            this.labelSet.TabIndex = 23;
            this.labelSet.Text = "<code>";
            // 
            // labelName
            // 
            this.labelName.AutoSize = true;
            this.labelName.Location = new System.Drawing.Point(3, 7);
            this.labelName.Name = "labelName";
            this.labelName.Size = new System.Drawing.Size(38, 13);
            this.labelName.TabIndex = 23;
            this.labelName.Text = "Name:";
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.panel_close);
            this.panelTop.Controls.Add(this.panel_minimize);
            this.panelTop.Controls.Add(this.label_index);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(3, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(862, 24);
            this.panelTop.TabIndex = 29;
            this.panelTop.MouseDown += new System.Windows.Forms.MouseEventHandler(this.label_index_MouseDown);
            this.panelTop.MouseUp += new System.Windows.Forms.MouseEventHandler(this.label_index_MouseUp);
            // 
            // panel_close
            // 
            this.panel_close.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.panel_close.AutoEllipsis = false;
            this.panel_close.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel_close.BorderColor = System.Drawing.Color.Orange;
            this.panel_close.BorderWidth = 1;
            this.panel_close.ButtonDisabledScaling = 0.25F;
            this.panel_close.Image = null;
            this.panel_close.ImageSelected = ExtendedControls.ExtButtonDrawn.ImageType.Close;
            this.panel_close.Location = new System.Drawing.Point(839, 0);
            this.panel_close.MouseOverColor = System.Drawing.Color.White;
            this.panel_close.MouseSelectedColor = System.Drawing.Color.Green;
            this.panel_close.MouseSelectedColorEnable = true;
            this.panel_close.Name = "panel_close";
            this.panel_close.Padding = new System.Windows.Forms.Padding(6);
            this.panel_close.Selectable = false;
            this.panel_close.Size = new System.Drawing.Size(24, 24);
            this.panel_close.TabIndex = 27;
            this.panel_close.TabStop = false;
            this.panel_close.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.panel_close.UseMnemonic = true;
            this.panel_close.Click += new System.EventHandler(this.panel_close_Click);
            // 
            // panel_minimize
            // 
            this.panel_minimize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.panel_minimize.AutoEllipsis = false;
            this.panel_minimize.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel_minimize.BorderColor = System.Drawing.Color.Orange;
            this.panel_minimize.BorderWidth = 1;
            this.panel_minimize.ButtonDisabledScaling = 0.25F;
            this.panel_minimize.Image = null;
            this.panel_minimize.ImageSelected = ExtendedControls.ExtButtonDrawn.ImageType.Minimize;
            this.panel_minimize.Location = new System.Drawing.Point(809, 0);
            this.panel_minimize.MouseOverColor = System.Drawing.Color.White;
            this.panel_minimize.MouseSelectedColor = System.Drawing.Color.Green;
            this.panel_minimize.MouseSelectedColorEnable = true;
            this.panel_minimize.Name = "panel_minimize";
            this.panel_minimize.Padding = new System.Windows.Forms.Padding(6);
            this.panel_minimize.Selectable = false;
            this.panel_minimize.Size = new System.Drawing.Size(24, 24);
            this.panel_minimize.TabIndex = 26;
            this.panel_minimize.TabStop = false;
            this.panel_minimize.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.panel_minimize.UseMnemonic = true;
            this.panel_minimize.Click += new System.EventHandler(this.panel_minimize_Click);
            // 
            // label_index
            // 
            this.label_index.AutoSize = true;
            this.label_index.Location = new System.Drawing.Point(3, 8);
            this.label_index.Name = "label_index";
            this.label_index.Size = new System.Drawing.Size(43, 13);
            this.label_index.TabIndex = 23;
            this.label_index.Text = "<code>";
            this.label_index.MouseDown += new System.Windows.Forms.MouseEventHandler(this.label_index_MouseDown);
            this.label_index.MouseUp += new System.Windows.Forms.MouseEventHandler(this.label_index_MouseUp);
            // 
            // panelOK
            // 
            this.panelOK.Controls.Add(this.extButtonHeader);
            this.panelOK.Controls.Add(this.buttonExtDisk);
            this.panelOK.Controls.Add(this.buttonExtLoad);
            this.panelOK.Controls.Add(this.buttonExtSave);
            this.panelOK.Controls.Add(this.buttonExtEdit);
            this.panelOK.Controls.Add(this.buttonCancel);
            this.panelOK.Controls.Add(this.buttonOK);
            this.panelOK.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelOK.Location = new System.Drawing.Point(3, 448);
            this.panelOK.Name = "panelOK";
            this.panelOK.Size = new System.Drawing.Size(862, 30);
            this.panelOK.TabIndex = 9;
            // 
            // extButtonHeader
            // 
            this.extButtonHeader.BackColor2 = System.Drawing.Color.Red;
            this.extButtonHeader.ButtonDisabledScaling = 0.5F;
            this.extButtonHeader.GradientDirection = 90F;
            this.extButtonHeader.Location = new System.Drawing.Point(374, 4);
            this.extButtonHeader.MouseOverScaling = 1.3F;
            this.extButtonHeader.MouseSelectedScaling = 1.3F;
            this.extButtonHeader.Name = "extButtonHeader";
            this.extButtonHeader.Size = new System.Drawing.Size(75, 23);
            this.extButtonHeader.TabIndex = 11;
            this.extButtonHeader.Text = "Header";
            this.extButtonHeader.UseVisualStyleBackColor = true;
            this.extButtonHeader.Click += new System.EventHandler(this.extButtonHeader_Click);
            // 
            // buttonExtDisk
            // 
            this.buttonExtDisk.BackColor2 = System.Drawing.Color.Red;
            this.buttonExtDisk.ButtonDisabledScaling = 0.5F;
            this.buttonExtDisk.GradientDirection = 90F;
            this.buttonExtDisk.Location = new System.Drawing.Point(275, 4);
            this.buttonExtDisk.MouseOverScaling = 1.3F;
            this.buttonExtDisk.MouseSelectedScaling = 1.3F;
            this.buttonExtDisk.Name = "buttonExtDisk";
            this.buttonExtDisk.Size = new System.Drawing.Size(75, 23);
            this.buttonExtDisk.TabIndex = 11;
            this.buttonExtDisk.Text = "As File";
            this.buttonExtDisk.UseVisualStyleBackColor = true;
            this.buttonExtDisk.Click += new System.EventHandler(this.buttonExtDisk_Click);
            // 
            // buttonExtLoad
            // 
            this.buttonExtLoad.BackColor2 = System.Drawing.Color.Red;
            this.buttonExtLoad.ButtonDisabledScaling = 0.5F;
            this.buttonExtLoad.GradientDirection = 90F;
            this.buttonExtLoad.Location = new System.Drawing.Point(170, 4);
            this.buttonExtLoad.MouseOverScaling = 1.3F;
            this.buttonExtLoad.MouseSelectedScaling = 1.3F;
            this.buttonExtLoad.Name = "buttonExtLoad";
            this.buttonExtLoad.Size = new System.Drawing.Size(75, 23);
            this.buttonExtLoad.TabIndex = 10;
            this.buttonExtLoad.Text = "Load";
            this.buttonExtLoad.UseVisualStyleBackColor = true;
            this.buttonExtLoad.Click += new System.EventHandler(this.buttonExtLoad_Click);
            // 
            // buttonExtSave
            // 
            this.buttonExtSave.BackColor2 = System.Drawing.Color.Red;
            this.buttonExtSave.ButtonDisabledScaling = 0.5F;
            this.buttonExtSave.GradientDirection = 90F;
            this.buttonExtSave.Location = new System.Drawing.Point(88, 4);
            this.buttonExtSave.MouseOverScaling = 1.3F;
            this.buttonExtSave.MouseSelectedScaling = 1.3F;
            this.buttonExtSave.Name = "buttonExtSave";
            this.buttonExtSave.Size = new System.Drawing.Size(75, 23);
            this.buttonExtSave.TabIndex = 9;
            this.buttonExtSave.Text = "Save";
            this.buttonExtSave.UseVisualStyleBackColor = true;
            this.buttonExtSave.Click += new System.EventHandler(this.buttonExtSave_Click);
            // 
            // buttonExtEdit
            // 
            this.buttonExtEdit.BackColor2 = System.Drawing.Color.Red;
            this.buttonExtEdit.ButtonDisabledScaling = 0.5F;
            this.buttonExtEdit.GradientDirection = 90F;
            this.buttonExtEdit.Location = new System.Drawing.Point(6, 4);
            this.buttonExtEdit.MouseOverScaling = 1.3F;
            this.buttonExtEdit.MouseSelectedScaling = 1.3F;
            this.buttonExtEdit.Name = "buttonExtEdit";
            this.buttonExtEdit.Size = new System.Drawing.Size(75, 23);
            this.buttonExtEdit.TabIndex = 8;
            this.buttonExtEdit.Text = "Text Edit";
            this.buttonExtEdit.UseVisualStyleBackColor = true;
            this.buttonExtEdit.Click += new System.EventHandler(this.buttonExtEdit_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.BackColor2 = System.Drawing.Color.Red;
            this.buttonCancel.ButtonDisabledScaling = 0.5F;
            this.buttonCancel.GradientDirection = 90F;
            this.buttonCancel.Location = new System.Drawing.Point(615, 4);
            this.buttonCancel.MouseOverScaling = 1.3F;
            this.buttonCancel.MouseSelectedScaling = 1.3F;
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(100, 23);
            this.buttonCancel.TabIndex = 6;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.BackColor2 = System.Drawing.Color.Red;
            this.buttonOK.ButtonDisabledScaling = 0.5F;
            this.buttonOK.GradientDirection = 90F;
            this.buttonOK.Location = new System.Drawing.Point(747, 4);
            this.buttonOK.MouseOverScaling = 1.3F;
            this.buttonOK.MouseSelectedScaling = 1.3F;
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(100, 23);
            this.buttonOK.TabIndex = 7;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem,
            this.deleteToolStripMenuItem,
            this.insertEntryAboveToolStripMenuItem,
            this.whitespaceToolStripMenuItem,
            this.removeWhitespaceToolStripMenuItem,
            this.editCommentToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(201, 158);
            this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.Size = new System.Drawing.Size(200, 22);
            this.copyToolStripMenuItem.Text = "Copy";
            this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
            // 
            // pasteToolStripMenuItem
            // 
            this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
            this.pasteToolStripMenuItem.Size = new System.Drawing.Size(200, 22);
            this.pasteToolStripMenuItem.Text = "Paste";
            this.pasteToolStripMenuItem.Click += new System.EventHandler(this.pasteToolStripMenuItem_Click);
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(200, 22);
            this.deleteToolStripMenuItem.Text = "Delete";
            this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
            // 
            // insertEntryAboveToolStripMenuItem
            // 
            this.insertEntryAboveToolStripMenuItem.Name = "insertEntryAboveToolStripMenuItem";
            this.insertEntryAboveToolStripMenuItem.Size = new System.Drawing.Size(200, 22);
            this.insertEntryAboveToolStripMenuItem.Text = "Insert Entry above";
            this.insertEntryAboveToolStripMenuItem.Click += new System.EventHandler(this.insertEntryAboveToolStripMenuItem_Click);
            // 
            // whitespaceToolStripMenuItem
            // 
            this.whitespaceToolStripMenuItem.Name = "whitespaceToolStripMenuItem";
            this.whitespaceToolStripMenuItem.Size = new System.Drawing.Size(200, 22);
            this.whitespaceToolStripMenuItem.Text = "Insert whitespace below";
            this.whitespaceToolStripMenuItem.Click += new System.EventHandler(this.whitespaceToolStripMenuItem_Click);
            // 
            // removeWhitespaceToolStripMenuItem
            // 
            this.removeWhitespaceToolStripMenuItem.Name = "removeWhitespaceToolStripMenuItem";
            this.removeWhitespaceToolStripMenuItem.Size = new System.Drawing.Size(200, 22);
            this.removeWhitespaceToolStripMenuItem.Text = "Remove whitespace";
            this.removeWhitespaceToolStripMenuItem.Click += new System.EventHandler(this.removeWhitespaceToolStripMenuItem_Click);
            // 
            // editCommentToolStripMenuItem
            // 
            this.editCommentToolStripMenuItem.Name = "editCommentToolStripMenuItem";
            this.editCommentToolStripMenuItem.Size = new System.Drawing.Size(200, 22);
            this.editCommentToolStripMenuItem.Text = "Edit Comment";
            this.editCommentToolStripMenuItem.Click += new System.EventHandler(this.editCommentToolStripMenuItem_Click);
            // 
            // toolTip1
            // 
            this.toolTip1.ShowAlways = true;
            // 
            // statusStripCustom
            // 
            this.statusStripCustom.Location = new System.Drawing.Point(3, 478);
            this.statusStripCustom.Name = "statusStripCustom";
            this.statusStripCustom.Size = new System.Drawing.Size(862, 22);
            this.statusStripCustom.TabIndex = 28;
            this.statusStripCustom.Text = "statusStripCustom1";
            // 
            // ActionProgramEditForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(868, 500);
            this.Controls.Add(this.panelOuter);
            this.Controls.Add(this.panelName);
            this.Controls.Add(this.panelTop);
            this.Controls.Add(this.panelOK);
            this.Controls.Add(this.statusStripCustom);
            this.Name = "ActionProgramEditForm";
            this.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Action Program";
            this.Shown += new System.EventHandler(this.ActionProgramForm_Shown);
            this.panelOuter.ResumeLayout(false);
            this.extPanelVertScrollWithBar.ResumeLayout(false);
            this.panelVScroll.ResumeLayout(false);
            this.panelName.ResumeLayout(false);
            this.panelName.PerformLayout();
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.panelOK.ResumeLayout(false);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panelOuter;
        private ExtendedControls.ExtPanelVertScroll panelVScroll;
        private ExtendedControls.ExtButton buttonMore;
        private ExtendedControls.ExtStatusStrip statusStripCustom;
        private System.Windows.Forms.Panel panelTop;
        private ExtendedControls.ExtButtonDrawn panel_close;
        private ExtendedControls.ExtButtonDrawn panel_minimize;
        private System.Windows.Forms.Label label_index;
        private System.Windows.Forms.Panel panelName;
        private ExtendedControls.ExtTextBox textBoxBorderName;
        private System.Windows.Forms.Label labelName;
        private System.Windows.Forms.Panel panelOK;
        private ExtendedControls.ExtButton buttonCancel;
        private ExtendedControls.ExtButton buttonOK;
        private ExtendedControls.ExtButton buttonExtDelete;
        private System.Windows.Forms.Label labelSet;
        private ExtendedControls.ExtButton buttonExtLoad;
        private ExtendedControls.ExtButton buttonExtSave;
        private ExtendedControls.ExtButton buttonExtEdit;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem whitespaceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeWhitespaceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem insertEntryAboveToolStripMenuItem;
        private System.Windows.Forms.ToolTip toolTip1;
        private ExtendedControls.ExtButton buttonExtDisk;
        private System.Windows.Forms.ToolStripMenuItem editCommentToolStripMenuItem;
        private ExtendedControls.ExtButton extButtonHeader;
        private ExtendedControls.ExtPanelVertScrollWithBar extPanelVertScrollWithBar;
    }
}
