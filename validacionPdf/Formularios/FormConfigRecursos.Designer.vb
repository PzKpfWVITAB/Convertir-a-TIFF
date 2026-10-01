<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormConfigRecursos
    Inherits System.Windows.Forms.Form

    <System.Diagnostics.DebuggerNonUserCode()>
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

    Friend WithEvents grpHardware As System.Windows.Forms.GroupBox
    Friend WithEvents lblCpu As System.Windows.Forms.Label
    Friend WithEvents lblRam As System.Windows.Forms.Label
    Friend WithEvents lblRecomendacion As System.Windows.Forms.Label
    Friend WithEvents grpConfiguracion As System.Windows.Forms.GroupBox
    Friend WithEvents rbAutomatico As System.Windows.Forms.RadioButton
    Friend WithEvents rbManual As System.Windows.Forms.RadioButton
    Friend WithEvents pnlManual As System.Windows.Forms.Panel
    Friend WithEvents lblHilos As System.Windows.Forms.Label
    Friend WithEvents nudHilos As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblMemoria As System.Windows.Forms.Label
    Friend WithEvents nudMemoria As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblStaging As System.Windows.Forms.Label
    Friend WithEvents txtStaging As System.Windows.Forms.TextBox
    Friend WithEvents btnExaminar As System.Windows.Forms.Button
    Friend WithEvents btnGuardar As System.Windows.Forms.Button
    Friend WithEvents btnRestablecer As System.Windows.Forms.Button
    Friend WithEvents btnCerrar As System.Windows.Forms.Button
    Friend WithEvents fbdStaging As System.Windows.Forms.FolderBrowserDialog

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.grpHardware = New System.Windows.Forms.GroupBox()
        Me.lblRecomendacion = New System.Windows.Forms.Label()
        Me.lblRam = New System.Windows.Forms.Label()
        Me.lblCpu = New System.Windows.Forms.Label()
        Me.grpConfiguracion = New System.Windows.Forms.GroupBox()
        Me.pnlManual = New System.Windows.Forms.Panel()
        Me.btnExaminar = New System.Windows.Forms.Button()
        Me.txtStaging = New System.Windows.Forms.TextBox()
        Me.lblStaging = New System.Windows.Forms.Label()
        Me.nudMemoria = New System.Windows.Forms.NumericUpDown()
        Me.lblMemoria = New System.Windows.Forms.Label()
        Me.nudHilos = New System.Windows.Forms.NumericUpDown()
        Me.lblHilos = New System.Windows.Forms.Label()
        Me.rbManual = New System.Windows.Forms.RadioButton()
        Me.rbAutomatico = New System.Windows.Forms.RadioButton()
        Me.btnGuardar = New System.Windows.Forms.Button()
        Me.btnRestablecer = New System.Windows.Forms.Button()
        Me.btnCerrar = New System.Windows.Forms.Button()
        Me.fbdStaging = New System.Windows.Forms.FolderBrowserDialog()
        Me.grpHardware.SuspendLayout()
        Me.grpConfiguracion.SuspendLayout()
        Me.pnlManual.SuspendLayout()
        CType(Me.nudMemoria, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudHilos, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'grpHardware
        '
        Me.grpHardware.Controls.Add(Me.lblRecomendacion)
        Me.grpHardware.Controls.Add(Me.lblRam)
        Me.grpHardware.Controls.Add(Me.lblCpu)
        Me.grpHardware.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.grpHardware.Location = New System.Drawing.Point(12, 12)
        Me.grpHardware.Name = "grpHardware"
        Me.grpHardware.Size = New System.Drawing.Size(560, 115)
        Me.grpHardware.TabIndex = 0
        Me.grpHardware.TabStop = False
        Me.grpHardware.Text = "Hardware Detectado en el Equipo"
        '
        'lblRecomendacion
        '
        Me.lblRecomendacion.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblRecomendacion.ForeColor = System.Drawing.Color.DarkGreen
        Me.lblRecomendacion.Location = New System.Drawing.Point(15, 80)
        Me.lblRecomendacion.Name = "lblRecomendacion"
        Me.lblRecomendacion.Size = New System.Drawing.Size(530, 25)
        Me.lblRecomendacion.TabIndex = 2
        Me.lblRecomendacion.Text = "Cálculo 90%: 14 hilos asignados | 10% margen libre reservado para el SO"
        '
        'lblRam
        '
        Me.lblRam.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblRam.Location = New System.Drawing.Point(15, 52)
        Me.lblRam.Name = "lblRam"
        Me.lblRam.Size = New System.Drawing.Size(530, 20)
        Me.lblRam.TabIndex = 1
        Me.lblRam.Text = "Memoria RAM: 32 GB total (16 GB libre)"
        '
        'lblCpu
        '
        Me.lblCpu.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblCpu.Location = New System.Drawing.Point(15, 25)
        Me.lblCpu.Name = "lblCpu"
        Me.lblCpu.Size = New System.Drawing.Size(530, 20)
        Me.lblCpu.TabIndex = 0
        Me.lblCpu.Text = "Procesador: AMD Ryzen 7 5700X (16 núcleos lógicos)"
        '
        'grpConfiguracion
        '
        Me.grpConfiguracion.Controls.Add(Me.pnlManual)
        Me.grpConfiguracion.Controls.Add(Me.rbManual)
        Me.grpConfiguracion.Controls.Add(Me.rbAutomatico)
        Me.grpConfiguracion.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.grpConfiguracion.Location = New System.Drawing.Point(12, 133)
        Me.grpConfiguracion.Name = "grpConfiguracion"
        Me.grpConfiguracion.Size = New System.Drawing.Size(560, 210)
        Me.grpConfiguracion.TabIndex = 1
        Me.grpConfiguracion.TabStop = False
        Me.grpConfiguracion.Text = "Configuración de Rendimiento"
        '
        'pnlManual
        '
        Me.pnlManual.Controls.Add(Me.btnExaminar)
        Me.pnlManual.Controls.Add(Me.txtStaging)
        Me.pnlManual.Controls.Add(Me.lblStaging)
        Me.pnlManual.Controls.Add(Me.nudMemoria)
        Me.pnlManual.Controls.Add(Me.lblMemoria)
        Me.pnlManual.Controls.Add(Me.nudHilos)
        Me.pnlManual.Controls.Add(Me.lblHilos)
        Me.pnlManual.Enabled = False
        Me.pnlManual.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.pnlManual.Location = New System.Drawing.Point(18, 80)
        Me.pnlManual.Name = "pnlManual"
        Me.pnlManual.Size = New System.Drawing.Size(527, 118)
        Me.pnlManual.TabIndex = 2
        '
        'btnExaminar
        '
        Me.btnExaminar.Location = New System.Drawing.Point(440, 77)
        Me.btnExaminar.Name = "btnExaminar"
        Me.btnExaminar.Size = New System.Drawing.Size(75, 25)
        Me.btnExaminar.TabIndex = 6
        Me.btnExaminar.Text = "Examinar..."
        Me.btnExaminar.UseVisualStyleBackColor = True
        '
        'txtStaging
        '
        Me.txtStaging.Location = New System.Drawing.Point(180, 78)
        Me.txtStaging.Name = "txtStaging"
        Me.txtStaging.Size = New System.Drawing.Size(254, 23)
        Me.txtStaging.TabIndex = 5
        Me.txtStaging.Text = "C:\MagickTempCache"
        '
        'lblStaging
        '
        Me.lblStaging.AutoSize = True
        Me.lblStaging.Location = New System.Drawing.Point(10, 81)
        Me.lblStaging.Name = "lblStaging"
        Me.lblStaging.Size = New System.Drawing.Size(155, 15)
        Me.lblStaging.TabIndex = 4
        Me.lblStaging.Text = "Carpeta Búfer Local (Staging):"
        '
        'nudMemoria
        '
        Me.nudMemoria.Location = New System.Drawing.Point(180, 44)
        Me.nudMemoria.Maximum = New Decimal(New Integer() {256, 0, 0, 0})
        Me.nudMemoria.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.nudMemoria.Name = "nudMemoria"
        Me.nudMemoria.Size = New System.Drawing.Size(90, 23)
        Me.nudMemoria.TabIndex = 3
        Me.nudMemoria.Value = New Decimal(New Integer() {16, 0, 0, 0})
        '
        'lblMemoria
        '
        Me.lblMemoria.AutoSize = True
        Me.lblMemoria.Location = New System.Drawing.Point(10, 47)
        Me.lblMemoria.Name = "lblMemoria"
        Me.lblMemoria.Size = New System.Drawing.Size(149, 15)
        Me.lblMemoria.TabIndex = 2
        Me.lblMemoria.Text = "Límite Memoria RAM (GB):"
        '
        'nudHilos
        '
        Me.nudHilos.Location = New System.Drawing.Point(180, 11)
        Me.nudHilos.Maximum = New Decimal(New Integer() {128, 0, 0, 0})
        Me.nudHilos.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.nudHilos.Name = "nudHilos"
        Me.nudHilos.Size = New System.Drawing.Size(90, 23)
        Me.nudHilos.TabIndex = 1
        Me.nudHilos.Value = New Decimal(New Integer() {14, 0, 0, 0})
        '
        'lblHilos
        '
        Me.lblHilos.AutoSize = True
        Me.lblHilos.Location = New System.Drawing.Point(10, 13)
        Me.lblHilos.Name = "lblHilos"
        Me.lblHilos.Size = New System.Drawing.Size(154, 15)
        Me.lblHilos.TabIndex = 0
        Me.lblHilos.Text = "Hilos Concurrentes de CPU:"
        '
        'rbManual
        '
        Me.rbManual.AutoSize = True
        Me.rbManual.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.rbManual.Location = New System.Drawing.Point(18, 55)
        Me.rbManual.Name = "rbManual"
        Me.rbManual.Size = New System.Drawing.Size(262, 19)
        Me.rbManual.TabIndex = 1
        Me.rbManual.Text = "Manual Personalizado (forzar valores fijos)"
        Me.rbManual.UseVisualStyleBackColor = True
        '
        'rbAutomatico
        '
        Me.rbAutomatico.AutoSize = True
        Me.rbAutomatico.Checked = True
        Me.rbAutomatico.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.rbAutomatico.Location = New System.Drawing.Point(18, 28)
        Me.rbAutomatico.Name = "rbAutomatico"
        Me.rbAutomatico.Size = New System.Drawing.Size(465, 19)
        Me.rbAutomatico.TabIndex = 0
        Me.rbAutomatico.TabStop = True
        Me.rbAutomatico.Text = "Automático (Usa hasta el 90% de CPU y RAM, dejando 10% libre - Recomendado)"
        Me.rbAutomatico.UseVisualStyleBackColor = True
        '
        'btnGuardar
        '
        Me.btnGuardar.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnGuardar.Location = New System.Drawing.Point(232, 355)
        Me.btnGuardar.Name = "btnGuardar"
        Me.btnGuardar.Size = New System.Drawing.Size(130, 32)
        Me.btnGuardar.TabIndex = 2
        Me.btnGuardar.Text = "Guardar y Aplicar"
        Me.btnGuardar.UseVisualStyleBackColor = True
        '
        'btnRestablecer
        '
        Me.btnRestablecer.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnRestablecer.Location = New System.Drawing.Point(86, 355)
        Me.btnRestablecer.Name = "btnRestablecer"
        Me.btnRestablecer.Size = New System.Drawing.Size(140, 32)
        Me.btnRestablecer.TabIndex = 3
        Me.btnRestablecer.Text = "Restablecer a Auto"
        Me.btnRestablecer.UseVisualStyleBackColor = True
        '
        'btnCerrar
        '
        Me.btnCerrar.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnCerrar.Location = New System.Drawing.Point(368, 355)
        Me.btnCerrar.Name = "btnCerrar"
        Me.btnCerrar.Size = New System.Drawing.Size(100, 32)
        Me.btnCerrar.TabIndex = 4
        Me.btnCerrar.Text = "Cerrar"
        Me.btnCerrar.UseVisualStyleBackColor = True
        '
        'FormConfigRecursos
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(584, 401)
        Me.Controls.Add(Me.btnCerrar)
        Me.Controls.Add(Me.btnRestablecer)
        Me.Controls.Add(Me.btnGuardar)
        Me.Controls.Add(Me.grpConfiguracion)
        Me.Controls.Add(Me.grpHardware)
        Me.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormConfigRecursos"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Configuración de Rendimiento y Recursos de Hardware"
        Me.grpHardware.ResumeLayout(False)
        Me.grpConfiguracion.ResumeLayout(False)
        Me.grpConfiguracion.PerformLayout()
        Me.pnlManual.ResumeLayout(False)
        Me.pnlManual.PerformLayout()
        CType(Me.nudMemoria, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudHilos, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
End Class
