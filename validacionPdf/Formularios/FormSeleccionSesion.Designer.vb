<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormSeleccionSesion
    Inherits System.Windows.Forms.Form

    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then components.Dispose()
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.lblTitulo = New System.Windows.Forms.Label()
        Me.lblUsuario = New System.Windows.Forms.Label()
        Me.lblDirTitulo = New System.Windows.Forms.Label()
        Me.lvwDirectorios = New System.Windows.Forms.ListView()
        Me.colDirNombre = New System.Windows.Forms.ColumnHeader()
        Me.colDirEstado = New System.Windows.Forms.ColumnHeader()
        Me.lblLotesTitulo = New System.Windows.Forms.Label()
        Me.lvwDelegaciones = New System.Windows.Forms.ListView()
        Me.colDelNombre = New System.Windows.Forms.ColumnHeader()
        Me.colDelId = New System.Windows.Forms.ColumnHeader()
        Me.lblEstadoLotes = New System.Windows.Forms.Label()
        Me.btnComenzar = New System.Windows.Forms.Button()
        Me.btnCancelar = New System.Windows.Forms.Button()
        Me.btnRefrescar = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'lblTitulo
        '
        Me.lblTitulo.Font = New System.Drawing.Font("Arial", 14.0!, System.Drawing.FontStyle.Bold)
        Me.lblTitulo.Location = New System.Drawing.Point(12, 12)
        Me.lblTitulo.Name = "lblTitulo"
        Me.lblTitulo.Size = New System.Drawing.Size(760, 30)
        Me.lblTitulo.TabIndex = 0
        Me.lblTitulo.Text = "Seleccionar directorio y lote de trabajo"
        Me.lblTitulo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'lblUsuario
        '
        Me.lblUsuario.AutoSize = True
        Me.lblUsuario.Font = New System.Drawing.Font("Arial Narrow", 11.0!)
        Me.lblUsuario.Location = New System.Drawing.Point(12, 47)
        Me.lblUsuario.Name = "lblUsuario"
        Me.lblUsuario.TabIndex = 1
        Me.lblUsuario.Text = "Usuario:"
        '
        'lblDirTitulo
        '
        Me.lblDirTitulo.Font = New System.Drawing.Font("Arial Narrow", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblDirTitulo.Location = New System.Drawing.Point(12, 75)
        Me.lblDirTitulo.Name = "lblDirTitulo"
        Me.lblDirTitulo.Size = New System.Drawing.Size(240, 22)
        Me.lblDirTitulo.TabIndex = 2
        Me.lblDirTitulo.Text = "Directorios disponibles:"
        '
        'lvwDirectorios
        '
        Me.lvwDirectorios.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.colDirNombre, Me.colDirEstado})
        Me.lvwDirectorios.FullRowSelect = True
        Me.lvwDirectorios.GridLines = True
        Me.lvwDirectorios.Location = New System.Drawing.Point(12, 100)
        Me.lvwDirectorios.MultiSelect = False
        Me.lvwDirectorios.Name = "lvwDirectorios"
        Me.lvwDirectorios.Size = New System.Drawing.Size(240, 90)
        Me.lvwDirectorios.TabIndex = 3
        Me.lvwDirectorios.UseCompatibleStateImageBehavior = False
        Me.lvwDirectorios.View = System.Windows.Forms.View.Details
        Me.colDirNombre.Text = "Directorio"
        Me.colDirNombre.Width = 90
        Me.colDirEstado.Text = "Estado"
        Me.colDirEstado.Width = 140
        '
        'lblLotesTitulo
        '
        Me.lblLotesTitulo.Font = New System.Drawing.Font("Arial Narrow", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblLotesTitulo.Location = New System.Drawing.Point(270, 75)
        Me.lblLotesTitulo.Name = "lblLotesTitulo"
        Me.lblLotesTitulo.Size = New System.Drawing.Size(502, 22)
        Me.lblLotesTitulo.TabIndex = 4
        Me.lblLotesTitulo.Text = "Delegaciones disponibles:"
        '
        'lvwDelegaciones
        '
        Me.lvwDelegaciones.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.colDelId, Me.colDelNombre})
        Me.lvwDelegaciones.FullRowSelect = True
        Me.lvwDelegaciones.GridLines = True
        Me.lvwDelegaciones.Location = New System.Drawing.Point(270, 100)
        Me.lvwDelegaciones.MultiSelect = False
        Me.lvwDelegaciones.Name = "lvwDelegaciones"
        Me.lvwDelegaciones.Size = New System.Drawing.Size(502, 330)
        Me.lvwDelegaciones.TabIndex = 5
        Me.lvwDelegaciones.UseCompatibleStateImageBehavior = False
        Me.lvwDelegaciones.View = System.Windows.Forms.View.Details
        Me.colDelId.Text = "ID"
        Me.colDelId.Width = 60
        Me.colDelNombre.Text = "Delegación"
        Me.colDelNombre.Width = 350
        '
        'lvwDirectorios height extended
        '
        Me.lvwDirectorios.Size = New System.Drawing.Size(240, 330)
        '
        'lblEstadoLotes
        '
        Me.lblEstadoLotes.Font = New System.Drawing.Font("Arial Narrow", 10.0!)
        Me.lblEstadoLotes.ForeColor = System.Drawing.Color.DimGray
        Me.lblEstadoLotes.Location = New System.Drawing.Point(270, 438)
        Me.lblEstadoLotes.Name = "lblEstadoLotes"
        Me.lblEstadoLotes.Size = New System.Drawing.Size(502, 20)
        Me.lblEstadoLotes.TabIndex = 6
        Me.lblEstadoLotes.Text = "Seleccione una delegación para buscar lote disponible."
        '
        'btnRefrescar
        '
        Me.btnRefrescar.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.btnRefrescar.Font = New System.Drawing.Font("Arial Narrow", 10.0!)
        Me.btnRefrescar.Location = New System.Drawing.Point(12, 438)
        Me.btnRefrescar.Name = "btnRefrescar"
        Me.btnRefrescar.Size = New System.Drawing.Size(100, 28)
        Me.btnRefrescar.TabIndex = 7
        Me.btnRefrescar.Text = "↻ Refrescar"
        Me.btnRefrescar.UseVisualStyleBackColor = True
        '
        'btnComenzar
        '
        Me.btnComenzar.BackColor = System.Drawing.Color.DarkGreen
        Me.btnComenzar.Enabled = False
        Me.btnComenzar.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.btnComenzar.Font = New System.Drawing.Font("Arial Narrow", 12.0!, System.Drawing.FontStyle.Bold)
        Me.btnComenzar.ForeColor = System.Drawing.Color.White
        Me.btnComenzar.Location = New System.Drawing.Point(500, 475)
        Me.btnComenzar.Name = "btnComenzar"
        Me.btnComenzar.Size = New System.Drawing.Size(150, 38)
        Me.btnComenzar.TabIndex = 8
        Me.btnComenzar.Text = "Comenzar ›"
        Me.btnComenzar.UseVisualStyleBackColor = False
        '
        'btnCancelar
        '
        Me.btnCancelar.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.btnCancelar.Font = New System.Drawing.Font("Arial Narrow", 11.0!)
        Me.btnCancelar.Location = New System.Drawing.Point(390, 475)
        Me.btnCancelar.Name = "btnCancelar"
        Me.btnCancelar.Size = New System.Drawing.Size(100, 38)
        Me.btnCancelar.TabIndex = 9
        Me.btnCancelar.Text = "Cancelar"
        Me.btnCancelar.UseVisualStyleBackColor = True
        '
        'FormSeleccionSesion
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(784, 531)
        Me.Controls.Add(Me.lblTitulo)
        Me.Controls.Add(Me.lblUsuario)
        Me.Controls.Add(Me.lblDirTitulo)
        Me.Controls.Add(Me.lvwDirectorios)
        Me.Controls.Add(Me.lblLotesTitulo)
        Me.Controls.Add(Me.lvwDelegaciones)
        Me.Controls.Add(Me.lblEstadoLotes)
        Me.Controls.Add(Me.btnRefrescar)
        Me.Controls.Add(Me.btnComenzar)
        Me.Controls.Add(Me.btnCancelar)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormSeleccionSesion"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Seleccionar sesión de trabajo"
        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Friend WithEvents lblTitulo As Label
    Friend WithEvents lblUsuario As Label
    Friend WithEvents lblDirTitulo As Label
    Friend WithEvents lvwDirectorios As ListView
    Friend WithEvents colDirNombre As ColumnHeader
    Friend WithEvents colDirEstado As ColumnHeader
    Friend WithEvents lblLotesTitulo As Label
    Friend WithEvents lvwDelegaciones As ListView
    Friend WithEvents colDelId As ColumnHeader
    Friend WithEvents colDelNombre As ColumnHeader
    Friend WithEvents lblEstadoLotes As Label
    Friend WithEvents btnComenzar As Button
    Friend WithEvents btnCancelar As Button
    Friend WithEvents btnRefrescar As Button
End Class
