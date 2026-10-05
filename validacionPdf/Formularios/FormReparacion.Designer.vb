<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FormReparacion
    Inherits System.Windows.Forms.Form

    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim dataGridViewCellStyle1 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim dataGridViewCellStyle2 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Me.panelSuperior = New System.Windows.Forms.Panel()
        Me.lblEstadoBd = New System.Windows.Forms.Label()
        Me.btnAbrirDestino = New System.Windows.Forms.Button()
        Me.btnExaminarDestino = New System.Windows.Forms.Button()
        Me.txtDestino = New System.Windows.Forms.TextBox()
        Me.lblDestino = New System.Windows.Forms.Label()
        Me.btnAbrirOrigen = New System.Windows.Forms.Button()
        Me.btnExaminarOrigen = New System.Windows.Forms.Button()
        Me.txtOrigen = New System.Windows.Forms.TextBox()
        Me.lblOrigen = New System.Windows.Forms.Label()
        Me.panelBotones = New System.Windows.Forms.Panel()
        Me.chkSoloIncompletos = New System.Windows.Forms.CheckBox()
        Me.btnDetener = New System.Windows.Forms.Button()
        Me.btnReparar = New System.Windows.Forms.Button()
        Me.btnAnalizar = New System.Windows.Forms.Button()
        Me.dgvArchivos = New System.Windows.Forms.DataGridView()
        Me.ColSeleccion = New System.Windows.Forms.DataGridViewCheckBoxColumn()
        Me.ColEstado = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColDocumento = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColPagsEsperadas = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColJpgsFisicos = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColEnBd = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColCarpetaDestino = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColCarpetaOrigen = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColDetalle = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.contextMenuDgv = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.menuItemAbrirDestino = New System.Windows.Forms.ToolStripMenuItem()
        Me.menuItemAbrirOrigen = New System.Windows.Forms.ToolStripMenuItem()
        Me.menuItemAbrirPdf = New System.Windows.Forms.ToolStripMenuItem()
        Me.toolStripSeparator1 = New System.Windows.Forms.ToolStripSeparator()
        Me.menuItemRepararUno = New System.Windows.Forms.ToolStripMenuItem()
        Me.panelInferior = New System.Windows.Forms.Panel()
        Me.lblStatus = New System.Windows.Forms.Label()
        Me.lblEstadisticas = New System.Windows.Forms.Label()
        Me.progressBar1 = New System.Windows.Forms.ProgressBar()
        Me.panelSuperior.SuspendLayout()
        Me.panelBotones.SuspendLayout()
        CType(Me.dgvArchivos, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.contextMenuDgv.SuspendLayout()
        Me.panelInferior.SuspendLayout()
        Me.SuspendLayout()
        '
        'panelSuperior
        '
        Me.panelSuperior.BackColor = System.Drawing.Color.FromArgb(CType(CType(245, Byte), Integer), CType(CType(247, Byte), Integer), CType(CType(250, Byte), Integer))
        Me.panelSuperior.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.panelSuperior.Controls.Add(Me.lblEstadoBd)
        Me.panelSuperior.Controls.Add(Me.btnAbrirDestino)
        Me.panelSuperior.Controls.Add(Me.btnExaminarDestino)
        Me.panelSuperior.Controls.Add(Me.txtDestino)
        Me.panelSuperior.Controls.Add(Me.lblDestino)
        Me.panelSuperior.Controls.Add(Me.btnAbrirOrigen)
        Me.panelSuperior.Controls.Add(Me.btnExaminarOrigen)
        Me.panelSuperior.Controls.Add(Me.txtOrigen)
        Me.panelSuperior.Controls.Add(Me.lblOrigen)
        Me.panelSuperior.Dock = System.Windows.Forms.DockStyle.Top
        Me.panelSuperior.Location = New System.Drawing.Point(0, 0)
        Me.panelSuperior.Name = "panelSuperior"
        Me.panelSuperior.Size = New System.Drawing.Size(1200, 105)
        Me.panelSuperior.TabIndex = 0
        '
        'lblEstadoBd
        '
        Me.lblEstadoBd.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblEstadoBd.AutoSize = True
        Me.lblEstadoBd.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.lblEstadoBd.ForeColor = System.Drawing.Color.FromArgb(CType(CType(40, Byte), Integer), CType(CType(120, Byte), Integer), CType(CType(40, Byte), Integer))
        Me.lblEstadoBd.Location = New System.Drawing.Point(920, 10)
        Me.lblEstadoBd.Name = "lblEstadoBd"
        Me.lblEstadoBd.Size = New System.Drawing.Size(155, 20)
        Me.lblEstadoBd.TabIndex = 8
        Me.lblEstadoBd.Text = "Base de Datos: Activa"
        '
        'btnAbrirDestino
        '
        Me.btnAbrirDestino.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnAbrirDestino.BackColor = System.Drawing.Color.White
        Me.btnAbrirDestino.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnAbrirDestino.Location = New System.Drawing.Point(1030, 60)
        Me.btnAbrirDestino.Name = "btnAbrirDestino"
        Me.btnAbrirDestino.Size = New System.Drawing.Size(145, 32)
        Me.btnAbrirDestino.TabIndex = 7
        Me.btnAbrirDestino.Text = "📂 Abrir Destino"
        Me.btnAbrirDestino.UseVisualStyleBackColor = False
        '
        'btnExaminarDestino
        '
        Me.btnExaminarDestino.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnExaminarDestino.BackColor = System.Drawing.Color.White
        Me.btnExaminarDestino.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnExaminarDestino.Location = New System.Drawing.Point(920, 60)
        Me.btnExaminarDestino.Name = "btnExaminarDestino"
        Me.btnExaminarDestino.Size = New System.Drawing.Size(100, 32)
        Me.btnExaminarDestino.TabIndex = 6
        Me.btnExaminarDestino.Text = "Examinar..."
        Me.btnExaminarDestino.UseVisualStyleBackColor = False
        '
        'txtDestino
        '
        Me.txtDestino.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtDestino.Font = New System.Drawing.Font("Segoe UI", 9.5!)
        Me.txtDestino.Location = New System.Drawing.Point(140, 62)
        Me.txtDestino.Name = "txtDestino"
        Me.txtDestino.Size = New System.Drawing.Size(765, 29)
        Me.txtDestino.TabIndex = 5
        '
        'lblDestino
        '
        Me.lblDestino.AutoSize = True
        Me.lblDestino.Font = New System.Drawing.Font("Segoe UI", 9.5!, System.Drawing.FontStyle.Bold)
        Me.lblDestino.Location = New System.Drawing.Point(15, 65)
        Me.lblDestino.Name = "lblDestino"
        Me.lblDestino.Size = New System.Drawing.Size(117, 21)
        Me.lblDestino.TabIndex = 4
        Me.lblDestino.Text = "Carpeta Destino:"
        '
        'btnAbrirOrigen
        '
        Me.btnAbrirOrigen.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnAbrirOrigen.BackColor = System.Drawing.Color.White
        Me.btnAbrirOrigen.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnAbrirOrigen.Location = New System.Drawing.Point(1030, 18)
        Me.btnAbrirOrigen.Name = "btnAbrirOrigen"
        Me.btnAbrirOrigen.Size = New System.Drawing.Size(145, 32)
        Me.btnAbrirOrigen.TabIndex = 3
        Me.btnAbrirOrigen.Text = "📂 Abrir Origen"
        Me.btnAbrirOrigen.UseVisualStyleBackColor = False
        '
        'btnExaminarOrigen
        '
        Me.btnExaminarOrigen.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnExaminarOrigen.BackColor = System.Drawing.Color.White
        Me.btnExaminarOrigen.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.btnExaminarOrigen.Location = New System.Drawing.Point(920, 18)
        Me.btnExaminarOrigen.Name = "btnExaminarOrigen"
        Me.btnExaminarOrigen.Size = New System.Drawing.Size(100, 32)
        Me.btnExaminarOrigen.TabIndex = 2
        Me.btnExaminarOrigen.Text = "Examinar..."
        Me.btnExaminarOrigen.UseVisualStyleBackColor = False
        '
        'txtOrigen
        '
        Me.txtOrigen.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtOrigen.Font = New System.Drawing.Font("Segoe UI", 9.5!)
        Me.txtOrigen.Location = New System.Drawing.Point(140, 20)
        Me.txtOrigen.Name = "txtOrigen"
        Me.txtOrigen.Size = New System.Drawing.Size(765, 29)
        Me.txtOrigen.TabIndex = 1
        '
        'lblOrigen
        '
        Me.lblOrigen.AutoSize = True
        Me.lblOrigen.Font = New System.Drawing.Font("Segoe UI", 9.5!, System.Drawing.FontStyle.Bold)
        Me.lblOrigen.Location = New System.Drawing.Point(15, 23)
        Me.lblOrigen.Name = "lblOrigen"
        Me.lblOrigen.Size = New System.Drawing.Size(113, 21)
        Me.lblOrigen.TabIndex = 0
        Me.lblOrigen.Text = "Carpeta Origen:"
        '
        'panelBotones
        '
        Me.panelBotones.BackColor = System.Drawing.Color.White
        Me.panelBotones.Controls.Add(Me.chkSoloIncompletos)
        Me.panelBotones.Controls.Add(Me.btnDetener)
        Me.panelBotones.Controls.Add(Me.btnReparar)
        Me.panelBotones.Controls.Add(Me.btnAnalizar)
        Me.panelBotones.Dock = System.Windows.Forms.DockStyle.Top
        Me.panelBotones.Location = New System.Drawing.Point(0, 105)
        Me.panelBotones.Name = "panelBotones"
        Me.panelBotones.Size = New System.Drawing.Size(1200, 60)
        Me.panelBotones.TabIndex = 1
        '
        'chkSoloIncompletos
        '
        Me.chkSoloIncompletos.AutoSize = True
        Me.chkSoloIncompletos.Checked = True
        Me.chkSoloIncompletos.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkSoloIncompletos.Font = New System.Drawing.Font("Segoe UI", 9.5!)
        Me.chkSoloIncompletos.Location = New System.Drawing.Point(620, 18)
        Me.chkSoloIncompletos.Name = "chkSoloIncompletos"
        Me.chkSoloIncompletos.Size = New System.Drawing.Size(340, 25)
        Me.chkSoloIncompletos.TabIndex = 3
        Me.chkSoloIncompletos.Text = "Mostrar solo incompletos, vacíos o faltantes"
        Me.chkSoloIncompletos.UseVisualStyleBackColor = True
        '
        'btnDetener
        '
        Me.btnDetener.BackColor = System.Drawing.Color.FromArgb(CType(CType(245, Byte), Integer), CType(CType(245, Byte), Integer), CType(CType(245, Byte), Integer))
        Me.btnDetener.Enabled = False
        Me.btnDetener.Font = New System.Drawing.Font("Segoe UI", 9.5!, System.Drawing.FontStyle.Bold)
        Me.btnDetener.ForeColor = System.Drawing.Color.Firebrick
        Me.btnDetener.Location = New System.Drawing.Point(490, 10)
        Me.btnDetener.Name = "btnDetener"
        Me.btnDetener.Size = New System.Drawing.Size(110, 40)
        Me.btnDetener.TabIndex = 2
        Me.btnDetener.Text = "⏹ Detener"
        Me.btnDetener.UseVisualStyleBackColor = False
        '
        'btnReparar
        '
        Me.btnReparar.BackColor = System.Drawing.Color.FromArgb(CType(CType(35, Byte), Integer), CType(CType(135, Byte), Integer), CType(CType(60, Byte), Integer))
        Me.btnReparar.Enabled = False
        Me.btnReparar.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnReparar.ForeColor = System.Drawing.Color.White
        Me.btnReparar.Location = New System.Drawing.Point(230, 10)
        Me.btnReparar.Name = "btnReparar"
        Me.btnReparar.Size = New System.Drawing.Size(250, 40)
        Me.btnReparar.TabIndex = 1
        Me.btnReparar.Text = "🛠️ Reparar Incompletos"
        Me.btnReparar.UseVisualStyleBackColor = False
        '
        'btnAnalizar
        '
        Me.btnAnalizar.BackColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(200, Byte), Integer))
        Me.btnAnalizar.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.btnAnalizar.ForeColor = System.Drawing.Color.White
        Me.btnAnalizar.Location = New System.Drawing.Point(15, 10)
        Me.btnAnalizar.Name = "btnAnalizar"
        Me.btnAnalizar.Size = New System.Drawing.Size(200, 40)
        Me.btnAnalizar.TabIndex = 0
        Me.btnAnalizar.Text = "🔍 1. Analizar Faltantes"
        Me.btnAnalizar.UseVisualStyleBackColor = False
        '
        'dgvArchivos
        '
        Me.dgvArchivos.AllowUserToAddRows = False
        Me.dgvArchivos.AllowUserToDeleteRows = False
        Me.dgvArchivos.AllowUserToOrderColumns = True
        Me.dgvArchivos.BackgroundColor = System.Drawing.Color.White
        dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
        dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(CType(CType(235, Byte), Integer), CType(CType(240, Byte), Integer), CType(CType(245, Byte), Integer))
        dataGridViewCellStyle1.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText
        dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight
        dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.[True]
        Me.dgvArchivos.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1
        Me.dgvArchivos.ColumnHeadersHeight = 32
        Me.dgvArchivos.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.ColSeleccion, Me.ColEstado, Me.ColDocumento, Me.ColPagsEsperadas, Me.ColJpgsFisicos, Me.ColEnBd, Me.ColCarpetaDestino, Me.ColCarpetaOrigen, Me.ColDetalle})
        Me.dgvArchivos.ContextMenuStrip = Me.contextMenuDgv
        dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
        dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window
        dataGridViewCellStyle2.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText
        dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(CType(CType(210, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(250, Byte), Integer))
        dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.Black
        dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.[False]
        Me.dgvArchivos.DefaultCellStyle = dataGridViewCellStyle2
        Me.dgvArchivos.Dock = System.Windows.Forms.DockStyle.Fill
        Me.dgvArchivos.EnableHeadersVisualStyles = False
        Me.dgvArchivos.Location = New System.Drawing.Point(0, 165)
        Me.dgvArchivos.Name = "dgvArchivos"
        Me.dgvArchivos.RowHeadersVisible = False
        Me.dgvArchivos.RowHeadersWidth = 30
        Me.dgvArchivos.RowTemplate.Height = 26
        Me.dgvArchivos.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.dgvArchivos.Size = New System.Drawing.Size(1200, 480)
        Me.dgvArchivos.TabIndex = 2
        '
        'ColSeleccion
        '
        Me.ColSeleccion.HeaderText = "Sel"
        Me.ColSeleccion.MinimumWidth = 40
        Me.ColSeleccion.Name = "ColSeleccion"
        Me.ColSeleccion.Width = 45
        '
        'ColEstado
        '
        Me.ColEstado.HeaderText = "Diagnóstico"
        Me.ColEstado.MinimumWidth = 140
        Me.ColEstado.Name = "ColEstado"
        Me.ColEstado.ReadOnly = True
        Me.ColEstado.Width = 160
        '
        'ColDocumento
        '
        Me.ColDocumento.HeaderText = "Documento PDF"
        Me.ColDocumento.MinimumWidth = 180
        Me.ColDocumento.Name = "ColDocumento"
        Me.ColDocumento.ReadOnly = True
        Me.ColDocumento.Width = 240
        '
        'ColPagsEsperadas
        '
        Me.ColPagsEsperadas.HeaderText = "Págs PDF"
        Me.ColPagsEsperadas.MinimumWidth = 70
        Me.ColPagsEsperadas.Name = "ColPagsEsperadas"
        Me.ColPagsEsperadas.ReadOnly = True
        Me.ColPagsEsperadas.Width = 80
        '
        'ColJpgsFisicos
        '
        Me.ColJpgsFisicos.HeaderText = "JPGs en Destino"
        Me.ColJpgsFisicos.MinimumWidth = 90
        Me.ColJpgsFisicos.Name = "ColJpgsFisicos"
        Me.ColJpgsFisicos.ReadOnly = True
        Me.ColJpgsFisicos.Width = 100
        '
        'ColEnBd
        '
        Me.ColEnBd.HeaderText = "En BD"
        Me.ColEnBd.MinimumWidth = 80
        Me.ColEnBd.Name = "ColEnBd"
        Me.ColEnBd.ReadOnly = True
        Me.ColEnBd.Width = 85
        '
        'ColCarpetaDestino
        '
        Me.ColCarpetaDestino.HeaderText = "Carpeta Destino (Física)"
        Me.ColCarpetaDestino.MinimumWidth = 150
        Me.ColCarpetaDestino.Name = "ColCarpetaDestino"
        Me.ColCarpetaDestino.ReadOnly = True
        Me.ColCarpetaDestino.Width = 200
        '
        'ColCarpetaOrigen
        '
        Me.ColCarpetaOrigen.HeaderText = "Carpeta Origen"
        Me.ColCarpetaOrigen.MinimumWidth = 150
        Me.ColCarpetaOrigen.Name = "ColCarpetaOrigen"
        Me.ColCarpetaOrigen.ReadOnly = True
        Me.ColCarpetaOrigen.Width = 160
        '
        'ColDetalle
        '
        Me.ColDetalle.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        Me.ColDetalle.HeaderText = "Detalle"
        Me.ColDetalle.MinimumWidth = 150
        Me.ColDetalle.Name = "ColDetalle"
        Me.ColDetalle.ReadOnly = True
        '
        'contextMenuDgv
        '
        Me.contextMenuDgv.ImageScalingSize = New System.Drawing.Size(20, 20)
        Me.contextMenuDgv.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.menuItemAbrirDestino, Me.menuItemAbrirOrigen, Me.menuItemAbrirPdf, Me.toolStripSeparator1, Me.menuItemRepararUno})
        Me.contextMenuDgv.Name = "contextMenuDgv"
        Me.contextMenuDgv.Size = New System.Drawing.Size(265, 106)
        '
        'menuItemAbrirDestino
        '
        Me.menuItemAbrirDestino.Name = "menuItemAbrirDestino"
        Me.menuItemAbrirDestino.Size = New System.Drawing.Size(264, 24)
        Me.menuItemAbrirDestino.Text = "📂 Abrir Carpeta Destino"
        '
        'menuItemAbrirOrigen
        '
        Me.menuItemAbrirOrigen.Name = "menuItemAbrirOrigen"
        Me.menuItemAbrirOrigen.Size = New System.Drawing.Size(264, 24)
        Me.menuItemAbrirOrigen.Text = "📂 Abrir Carpeta Origen"
        '
        'menuItemAbrirPdf
        '
        Me.menuItemAbrirPdf.Name = "menuItemAbrirPdf"
        Me.menuItemAbrirPdf.Size = New System.Drawing.Size(264, 24)
        Me.menuItemAbrirPdf.Text = "📄 Abrir Documento PDF"
        '
        'toolStripSeparator1
        '
        Me.toolStripSeparator1.Name = "toolStripSeparator1"
        Me.toolStripSeparator1.Size = New System.Drawing.Size(261, 6)
        '
        'menuItemRepararUno
        '
        Me.menuItemRepararUno.Name = "menuItemRepararUno"
        Me.menuItemRepararUno.Size = New System.Drawing.Size(264, 24)
        Me.menuItemRepararUno.Text = "🛠️ Reparar solo este archivo"
        '
        'panelInferior
        '
        Me.panelInferior.BackColor = System.Drawing.Color.FromArgb(CType(CType(245, Byte), Integer), CType(CType(247, Byte), Integer), CType(CType(250, Byte), Integer))
        Me.panelInferior.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.panelInferior.Controls.Add(Me.lblStatus)
        Me.panelInferior.Controls.Add(Me.lblEstadisticas)
        Me.panelInferior.Controls.Add(Me.progressBar1)
        Me.panelInferior.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.panelInferior.Location = New System.Drawing.Point(0, 645)
        Me.panelInferior.Name = "panelInferior"
        Me.panelInferior.Size = New System.Drawing.Size(1200, 75)
        Me.panelInferior.TabIndex = 3
        '
        'lblStatus
        '
        Me.lblStatus.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblStatus.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblStatus.Location = New System.Drawing.Point(15, 45)
        Me.lblStatus.Name = "lblStatus"
        Me.lblStatus.Size = New System.Drawing.Size(1165, 20)
        Me.lblStatus.TabIndex = 2
        Me.lblStatus.Text = "Listo para iniciar el análisis."
        '
        'lblEstadisticas
        '
        Me.lblEstadisticas.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblEstadisticas.Font = New System.Drawing.Font("Segoe UI", 9.5!, System.Drawing.FontStyle.Bold)
        Me.lblEstadisticas.Location = New System.Drawing.Point(15, 10)
        Me.lblEstadisticas.Name = "lblEstadisticas"
        Me.lblEstadisticas.Size = New System.Drawing.Size(800, 25)
        Me.lblEstadisticas.TabIndex = 1
        Me.lblEstadisticas.Text = "Analizados: 0 | Correctos: 0 | Incompletos: 0 | Faltantes: 0"
        '
        'progressBar1
        '
        Me.progressBar1.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.progressBar1.Location = New System.Drawing.Point(830, 10)
        Me.progressBar1.Name = "progressBar1"
        Me.progressBar1.Size = New System.Drawing.Size(350, 24)
        Me.progressBar1.TabIndex = 0
        '
        'FormReparacion
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1200, 720)
        Me.Controls.Add(Me.dgvArchivos)
        Me.Controls.Add(Me.panelInferior)
        Me.Controls.Add(Me.panelBotones)
        Me.Controls.Add(Me.panelSuperior)
        Me.MinimumSize = New System.Drawing.Size(900, 500)
        Me.Name = "FormReparacion"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Herramienta de Diagnóstico y Reparación de Archivos (PDF a JPG 200 DPI)"
        Me.panelSuperior.ResumeLayout(False)
        Me.panelSuperior.PerformLayout()
        Me.panelBotones.ResumeLayout(False)
        Me.panelBotones.PerformLayout()
        CType(Me.dgvArchivos, System.ComponentModel.ISupportInitialize).EndInit()
        Me.contextMenuDgv.ResumeLayout(False)
        Me.panelInferior.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents panelSuperior As Panel
    Friend WithEvents lblOrigen As Label
    Friend WithEvents txtOrigen As TextBox
    Friend WithEvents btnExaminarOrigen As Button
    Friend WithEvents btnAbrirOrigen As Button
    Friend WithEvents lblDestino As Label
    Friend WithEvents txtDestino As TextBox
    Friend WithEvents btnExaminarDestino As Button
    Friend WithEvents btnAbrirDestino As Button
    Friend WithEvents lblEstadoBd As Label
    Friend WithEvents panelBotones As Panel
    Friend WithEvents btnAnalizar As Button
    Friend WithEvents btnReparar As Button
    Friend WithEvents btnDetener As Button
    Friend WithEvents chkSoloIncompletos As CheckBox
    Friend WithEvents dgvArchivos As DataGridView
    Friend WithEvents panelInferior As Panel
    Friend WithEvents progressBar1 As ProgressBar
    Friend WithEvents lblEstadisticas As Label
    Friend WithEvents lblStatus As Label
    Friend WithEvents contextMenuDgv As ContextMenuStrip
    Friend WithEvents menuItemAbrirDestino As ToolStripMenuItem
    Friend WithEvents menuItemAbrirOrigen As ToolStripMenuItem
    Friend WithEvents menuItemAbrirPdf As ToolStripMenuItem
    Friend WithEvents toolStripSeparator1 As ToolStripSeparator
    Friend WithEvents menuItemRepararUno As ToolStripMenuItem
    Friend WithEvents ColSeleccion As DataGridViewCheckBoxColumn
    Friend WithEvents ColEstado As DataGridViewTextBoxColumn
    Friend WithEvents ColDocumento As DataGridViewTextBoxColumn
    Friend WithEvents ColPagsEsperadas As DataGridViewTextBoxColumn
    Friend WithEvents ColJpgsFisicos As DataGridViewTextBoxColumn
    Friend WithEvents ColEnBd As DataGridViewTextBoxColumn
    Friend WithEvents ColCarpetaDestino As DataGridViewTextBoxColumn
    Friend WithEvents ColCarpetaOrigen As DataGridViewTextBoxColumn
    Friend WithEvents ColDetalle As DataGridViewTextBoxColumn
End Class
