<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormLogin
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
        Me.lblTitulo = New System.Windows.Forms.Label()
        Me.lblUsuario = New System.Windows.Forms.Label()
        Me.lblPin = New System.Windows.Forms.Label()
        Me.txtUsuario = New System.Windows.Forms.TextBox()
        Me.txtPin = New System.Windows.Forms.TextBox()
        Me.btnIniciar = New System.Windows.Forms.Button()
        Me.lblError = New System.Windows.Forms.Label()
        Me.msLogin = New System.Windows.Forms.MenuStrip()
        Me.itemOpciones = New System.Windows.Forms.ToolStripMenuItem()
        Me.itemUsuarios = New System.Windows.Forms.ToolStripMenuItem()
        Me.itemAnios = New System.Windows.Forms.ToolStripMenuItem()
        Me.itemDelegaciones = New System.Windows.Forms.ToolStripMenuItem()
        Me.itemAuditoria = New System.Windows.Forms.ToolStripMenuItem()
        Me.itemRecursos = New System.Windows.Forms.ToolStripMenuItem()
        Me.itemReparacion = New System.Windows.Forms.ToolStripMenuItem()
        Me.ButtonLotes = New System.Windows.Forms.Button()
        Me.btnTransferirIrec = New System.Windows.Forms.Button()
        Me.msLogin.SuspendLayout()
        Me.SuspendLayout()
        '
        'lblTitulo
        '
        Me.lblTitulo.Font = New System.Drawing.Font("Arial", 18.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblTitulo.Location = New System.Drawing.Point(20, 45)
        Me.lblTitulo.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTitulo.Name = "lblTitulo"
        Me.lblTitulo.Size = New System.Drawing.Size(580, 43)
        Me.lblTitulo.TabIndex = 0
        Me.lblTitulo.Text = "Validación PDF"
        Me.lblTitulo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'lblUsuario
        '
        Me.lblUsuario.AutoSize = True
        Me.lblUsuario.Font = New System.Drawing.Font("Arial Narrow", 14.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblUsuario.Location = New System.Drawing.Point(60, 115)
        Me.lblUsuario.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblUsuario.Name = "lblUsuario"
        Me.lblUsuario.Size = New System.Drawing.Size(86, 29)
        Me.lblUsuario.TabIndex = 1
        Me.lblUsuario.Text = "Usuario:"
        '
        'lblPin
        '
        Me.lblPin.AutoSize = True
        Me.lblPin.Font = New System.Drawing.Font("Arial Narrow", 14.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblPin.Location = New System.Drawing.Point(60, 165)
        Me.lblPin.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblPin.Name = "lblPin"
        Me.lblPin.Size = New System.Drawing.Size(49, 29)
        Me.lblPin.TabIndex = 2
        Me.lblPin.Text = "PIN:"
        '
        'txtUsuario
        '
        Me.txtUsuario.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtUsuario.Font = New System.Drawing.Font("Arial Narrow", 14.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtUsuario.Location = New System.Drawing.Point(180, 112)
        Me.txtUsuario.Margin = New System.Windows.Forms.Padding(4)
        Me.txtUsuario.Name = "txtUsuario"
        Me.txtUsuario.Size = New System.Drawing.Size(280, 35)
        Me.txtUsuario.TabIndex = 3
        '
        'txtPin
        '
        Me.txtPin.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtPin.Font = New System.Drawing.Font("Arial Narrow", 14.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtPin.Location = New System.Drawing.Point(180, 162)
        Me.txtPin.Margin = New System.Windows.Forms.Padding(4)
        Me.txtPin.MaxLength = 4
        Me.txtPin.Name = "txtPin"
        Me.txtPin.PasswordChar = Global.Microsoft.VisualBasic.ChrW(42)
        Me.txtPin.Size = New System.Drawing.Size(140, 35)
        Me.txtPin.TabIndex = 4
        '
        'btnIniciar
        '
        Me.btnIniciar.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.btnIniciar.Font = New System.Drawing.Font("Arial Narrow", 14.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnIniciar.Location = New System.Drawing.Point(60, 240)
        Me.btnIniciar.Margin = New System.Windows.Forms.Padding(4)
        Me.btnIniciar.Name = "btnIniciar"
        Me.btnIniciar.Size = New System.Drawing.Size(500, 45)
        Me.btnIniciar.TabIndex = 5
        Me.btnIniciar.Text = "Iniciar sesión"
        Me.btnIniciar.UseVisualStyleBackColor = True
        '
        'lblError
        '
        Me.lblError.Font = New System.Drawing.Font("Arial Narrow", 11.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblError.ForeColor = System.Drawing.Color.Red
        Me.lblError.Location = New System.Drawing.Point(20, 205)
        Me.lblError.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblError.Name = "lblError"
        Me.lblError.Size = New System.Drawing.Size(580, 25)
        Me.lblError.TabIndex = 7
        Me.lblError.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'msLogin
        '
        Me.msLogin.ImageScalingSize = New System.Drawing.Size(20, 20)
        Me.msLogin.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.itemOpciones})
        Me.msLogin.Location = New System.Drawing.Point(0, 0)
        Me.msLogin.Name = "msLogin"
        Me.msLogin.Size = New System.Drawing.Size(620, 28)
        Me.msLogin.TabIndex = 8
        Me.msLogin.Text = "MenuStrip1"
        '
        'itemOpciones
        '
        Me.itemOpciones.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.itemUsuarios, Me.itemAnios, Me.itemDelegaciones, Me.itemAuditoria, Me.itemRecursos, Me.itemReparacion})
        Me.itemOpciones.Name = "itemOpciones"
        Me.itemOpciones.Size = New System.Drawing.Size(179, 24)
        Me.itemOpciones.Text = "⚙ Opciones de Admin"
        '
        'itemUsuarios
        '
        Me.itemUsuarios.Name = "itemUsuarios"
        Me.itemUsuarios.Size = New System.Drawing.Size(268, 26)
        Me.itemUsuarios.Text = "Administrar Usuarios"
        '
        'itemAnios
        '
        Me.itemAnios.Name = "itemAnios"
        Me.itemAnios.Size = New System.Drawing.Size(268, 26)
        Me.itemAnios.Text = "Catálogo de Años"
        '
        'itemDelegaciones
        '
        Me.itemDelegaciones.Name = "itemDelegaciones"
        Me.itemDelegaciones.Size = New System.Drawing.Size(268, 26)
        Me.itemDelegaciones.Text = "Catálogo de Delegaciones"
        '
        'itemAuditoria
        '
        Me.itemAuditoria.Name = "itemAuditoria"
        Me.itemAuditoria.Size = New System.Drawing.Size(268, 26)
        Me.itemAuditoria.Text = "Ver Auditoría"
        '
        'itemRecursos
        '
        Me.itemRecursos.Name = "itemRecursos"
        Me.itemRecursos.Size = New System.Drawing.Size(268, 26)
        Me.itemRecursos.Text = "Rendimiento y Recursos (CPU/RAM)"
        '
        'itemReparacion
        '
        Me.itemReparacion.Name = "itemReparacion"
        Me.itemReparacion.Size = New System.Drawing.Size(268, 26)
        Me.itemReparacion.Text = "🛠️ Reparar Archivos Incompletos (JPG)"
        '
        'ButtonLotes
        '
        Me.ButtonLotes.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.ButtonLotes.Font = New System.Drawing.Font("Arial Narrow", 13.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ButtonLotes.Location = New System.Drawing.Point(60, 300)
        Me.ButtonLotes.Margin = New System.Windows.Forms.Padding(4)
        Me.ButtonLotes.Name = "ButtonLotes"
        Me.ButtonLotes.Size = New System.Drawing.Size(240, 45)
        Me.ButtonLotes.TabIndex = 9
        Me.ButtonLotes.Text = "Ingresar a Lotes"
        Me.ButtonLotes.UseVisualStyleBackColor = True
        '
        'btnTransferirIrec
        '
        Me.btnTransferirIrec.Cursor = System.Windows.Forms.Cursors.Default
        Me.btnTransferirIrec.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.btnTransferirIrec.Font = New System.Drawing.Font("Arial Narrow", 13.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnTransferirIrec.Location = New System.Drawing.Point(320, 300)
        Me.btnTransferirIrec.Margin = New System.Windows.Forms.Padding(4)
        Me.btnTransferirIrec.Name = "btnTransferirIrec"
        Me.btnTransferirIrec.Size = New System.Drawing.Size(240, 45)
        Me.btnTransferirIrec.TabIndex = 10
        Me.btnTransferirIrec.Text = "Transferir IREC a SSD"
        Me.btnTransferirIrec.UseVisualStyleBackColor = True
        '
        'FormLogin
        '
        Me.AcceptButton = Me.btnIniciar
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(620, 375)
        Me.Controls.Add(Me.btnTransferirIrec)
        Me.Controls.Add(Me.ButtonLotes)
        Me.Controls.Add(Me.lblError)
        Me.Controls.Add(Me.btnIniciar)
        Me.Controls.Add(Me.txtPin)
        Me.Controls.Add(Me.txtUsuario)
        Me.Controls.Add(Me.lblPin)
        Me.Controls.Add(Me.lblUsuario)
        Me.Controls.Add(Me.lblTitulo)
        Me.Controls.Add(Me.msLogin)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MainMenuStrip = Me.msLogin
        Me.Margin = New System.Windows.Forms.Padding(4)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormLogin"
        Me.ShowInTaskbar = True
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Iniciar Sesión"
        Me.msLogin.ResumeLayout(False)
        Me.msLogin.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents lblTitulo As Label
    Friend WithEvents lblUsuario As Label
    Friend WithEvents lblPin As Label
    Friend WithEvents txtUsuario As TextBox
    Friend WithEvents txtPin As TextBox
    Friend WithEvents btnIniciar As Button
    Friend WithEvents lblError As Label
    Friend WithEvents msLogin As MenuStrip
    Friend WithEvents itemOpciones As ToolStripMenuItem
    Friend WithEvents itemUsuarios As ToolStripMenuItem
    Friend WithEvents itemAnios As ToolStripMenuItem
    Friend WithEvents itemDelegaciones As ToolStripMenuItem
    Friend WithEvents itemAuditoria As ToolStripMenuItem
    Friend WithEvents itemRecursos As ToolStripMenuItem
    Friend WithEvents itemReparacion As ToolStripMenuItem
    Friend WithEvents ButtonLotes As Button
    Friend WithEvents btnTransferirIrec As Button
End Class
