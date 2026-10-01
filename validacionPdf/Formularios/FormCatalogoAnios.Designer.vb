<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormCatalogoAnios
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
        Me.lstAnios = New System.Windows.Forms.ListBox()
        Me.lblTitulo = New System.Windows.Forms.Label()
        Me.nudAnio = New System.Windows.Forms.NumericUpDown()
        Me.btnAgregar = New System.Windows.Forms.Button()
        Me.btnEliminar = New System.Windows.Forms.Button()
        Me.btnCerrar = New System.Windows.Forms.Button()
        CType(Me.nudAnio, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'lstAnios
        '
        Me.lstAnios.Font = New System.Drawing.Font("Arial Narrow", 14.25!)
        Me.lstAnios.FormattingEnabled = True
        Me.lstAnios.ItemHeight = 23
        Me.lstAnios.Location = New System.Drawing.Point(12, 45)
        Me.lstAnios.Name = "lstAnios"
        Me.lstAnios.Size = New System.Drawing.Size(150, 188)
        Me.lstAnios.TabIndex = 0
        '
        'lblTitulo
        '
        Me.lblTitulo.AutoSize = True
        Me.lblTitulo.Font = New System.Drawing.Font("Arial Narrow", 14.25!, System.Drawing.FontStyle.Bold)
        Me.lblTitulo.Location = New System.Drawing.Point(12, 15)
        Me.lblTitulo.Name = "lblTitulo"
        Me.lblTitulo.Size = New System.Drawing.Size(151, 23)
        Me.lblTitulo.TabIndex = 1
        Me.lblTitulo.Text = "Años válidos"
        '
        'nudAnio
        '
        Me.nudAnio.Font = New System.Drawing.Font("Arial Narrow", 14.25!)
        Me.nudAnio.Location = New System.Drawing.Point(190, 55)
        Me.nudAnio.Maximum = New Decimal(New Integer() {2100, 0, 0, 0})
        Me.nudAnio.Minimum = New Decimal(New Integer() {1990, 0, 0, 0})
        Me.nudAnio.Name = "nudAnio"
        Me.nudAnio.Size = New System.Drawing.Size(100, 29)
        Me.nudAnio.TabIndex = 2
        Me.nudAnio.Value = New Decimal(New Integer() {2026, 0, 0, 0})
        '
        'btnAgregar
        '
        Me.btnAgregar.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.btnAgregar.Font = New System.Drawing.Font("Arial Narrow", 12.0!)
        Me.btnAgregar.Location = New System.Drawing.Point(190, 95)
        Me.btnAgregar.Name = "btnAgregar"
        Me.btnAgregar.Size = New System.Drawing.Size(100, 35)
        Me.btnAgregar.TabIndex = 3
        Me.btnAgregar.Text = "Agregar"
        Me.btnAgregar.UseVisualStyleBackColor = True
        '
        'btnEliminar
        '
        Me.btnEliminar.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.btnEliminar.Font = New System.Drawing.Font("Arial Narrow", 12.0!)
        Me.btnEliminar.Location = New System.Drawing.Point(190, 140)
        Me.btnEliminar.Name = "btnEliminar"
        Me.btnEliminar.Size = New System.Drawing.Size(100, 35)
        Me.btnEliminar.TabIndex = 4
        Me.btnEliminar.Text = "Eliminar"
        Me.btnEliminar.UseVisualStyleBackColor = True
        '
        'btnCerrar
        '
        Me.btnCerrar.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.btnCerrar.Font = New System.Drawing.Font("Arial Narrow", 12.0!)
        Me.btnCerrar.Location = New System.Drawing.Point(190, 200)
        Me.btnCerrar.Name = "btnCerrar"
        Me.btnCerrar.Size = New System.Drawing.Size(100, 35)
        Me.btnCerrar.TabIndex = 5
        Me.btnCerrar.Text = "Cerrar"
        Me.btnCerrar.UseVisualStyleBackColor = True
        '
        'FormCatalogoAnios
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(304, 251)
        Me.Controls.Add(Me.btnCerrar)
        Me.Controls.Add(Me.btnEliminar)
        Me.Controls.Add(Me.btnAgregar)
        Me.Controls.Add(Me.nudAnio)
        Me.Controls.Add(Me.lblTitulo)
        Me.Controls.Add(Me.lstAnios)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormCatalogoAnios"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Catálogo de Años"
        CType(Me.nudAnio, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Friend WithEvents lstAnios As ListBox
    Friend WithEvents lblTitulo As Label
    Friend WithEvents nudAnio As NumericUpDown
    Friend WithEvents btnAgregar As Button
    Friend WithEvents btnEliminar As Button
    Friend WithEvents btnCerrar As Button
End Class
