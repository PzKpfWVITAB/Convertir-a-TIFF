<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormCargando
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

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.lblMensaje = New System.Windows.Forms.Label()
        Me.picCarga = New System.Windows.Forms.PictureBox()
        Me.timerCarga = New System.Windows.Forms.Timer(Me.components)
        CType(Me.picCarga, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'lblMensaje
        '
        Me.lblMensaje.Font = New System.Drawing.Font("Arial", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblMensaje.Location = New System.Drawing.Point(12, 110)
        Me.lblMensaje.Name = "lblMensaje"
        Me.lblMensaje.Size = New System.Drawing.Size(276, 30)
        Me.lblMensaje.TabIndex = 0
        Me.lblMensaje.Text = "Buscando PDFs en la red..."
        Me.lblMensaje.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'picCarga
        '
        Me.picCarga.Location = New System.Drawing.Point(110, 20)
        Me.picCarga.Name = "picCarga"
        Me.picCarga.Size = New System.Drawing.Size(80, 80)
        Me.picCarga.TabIndex = 1
        Me.picCarga.TabStop = False
        '
        'timerCarga
        '
        Me.timerCarga.Enabled = True
        Me.timerCarga.Interval = 30
        '
        'FormCargando
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.White
        Me.ClientSize = New System.Drawing.Size(300, 160)
        Me.ControlBox = False
        Me.Controls.Add(Me.picCarga)
        Me.Controls.Add(Me.lblMensaje)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.Name = "FormCargando"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Cargando"
        CType(Me.picCarga, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents lblMensaje As Label
    Friend WithEvents picCarga As PictureBox
    Friend WithEvents timerCarga As Timer
End Class
