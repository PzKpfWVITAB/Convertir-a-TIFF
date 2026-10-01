<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form reemplaza a Dispose para limpiar la lista de componentes.
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

    'Requerido por el Diseñador de Windows Forms
    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.btnRenombrar = New System.Windows.Forms.Button()
        Me.lblEstado = New System.Windows.Forms.Label()
        Me.txtNombre = New System.Windows.Forms.TextBox()
        Me.btnSeleccionarCarpeta = New System.Windows.Forms.Button()
        Me.btnSiguiente = New System.Windows.Forms.Button()
        Me.lblNombreActual = New System.Windows.Forms.Label()
        Me.lblContador = New System.Windows.Forms.Panel()
        Me.lblCaptura = New System.Windows.Forms.Label()
        Me.btnEliminar = New System.Windows.Forms.Button()
        Me.lblTotalPdf = New System.Windows.Forms.Label()
        Me.lbl = New System.Windows.Forms.Label()
        Me.Splitter1 = New System.Windows.Forms.Splitter()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.btnDividir = New System.Windows.Forms.Button()
        Me.txtBloques = New System.Windows.Forms.TextBox()
        Me.rbRegla2 = New System.Windows.Forms.RadioButton()
        Me.rbRegla1 = New System.Windows.Forms.RadioButton()
        Me.btnDuplicar = New System.Windows.Forms.Button()
        Me.WebView21 = New Microsoft.Web.WebView2.WinForms.WebView2()
        ' Controles de rotación
        Me.lblRotacion = New System.Windows.Forms.Label()
        Me.nudPagina = New System.Windows.Forms.NumericUpDown()
        Me.lblPagina = New System.Windows.Forms.Label()
        Me.btnRotarIzquierda = New System.Windows.Forms.Button()
        Me.btnRotarDerecha = New System.Windows.Forms.Button()
        Me.btnCerrarSesion = New System.Windows.Forms.Button()
        Me.lblContador.SuspendLayout()
        CType(Me.WebView21, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudPagina, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'btnRenombrar
        '
        Me.btnRenombrar.BackColor = System.Drawing.SystemColors.ButtonFace
        Me.btnRenombrar.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.btnRenombrar.Font = New System.Drawing.Font("Arial Narrow", 14.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnRenombrar.Location = New System.Drawing.Point(584, 116)
        Me.btnRenombrar.Name = "btnRenombrar"
        Me.btnRenombrar.Size = New System.Drawing.Size(105, 35)
        Me.btnRenombrar.TabIndex = 1
        Me.btnRenombrar.Text = "Renombrar"
        Me.btnRenombrar.UseVisualStyleBackColor = False
        '
        'lblEstado
        '
        Me.lblEstado.AutoSize = True
        Me.lblEstado.Font = New System.Drawing.Font("Arial Narrow", 14.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblEstado.Location = New System.Drawing.Point(261, 80)
        Me.lblEstado.Name = "lblEstado"
        Me.lblEstado.Size = New System.Drawing.Size(58, 23)
        Me.lblEstado.TabIndex = 2
        Me.lblEstado.Text = "Estado"
        '
        'txtNombre
        '
        Me.txtNombre.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtNombre.Font = New System.Drawing.Font("Arial Narrow", 15.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtNombre.Location = New System.Drawing.Point(265, 119)
        Me.txtNombre.Name = "txtNombre"
        Me.txtNombre.Size = New System.Drawing.Size(297, 32)
        Me.txtNombre.TabIndex = 3
        '
        'btnSeleccionarCarpeta
        '
        Me.btnSeleccionarCarpeta.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.btnSeleccionarCarpeta.Font = New System.Drawing.Font("Arial Narrow", 14.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnSeleccionarCarpeta.Location = New System.Drawing.Point(12, 12)
        Me.btnSeleccionarCarpeta.Name = "btnSeleccionarCarpeta"
        Me.btnSeleccionarCarpeta.Size = New System.Drawing.Size(212, 35)
        Me.btnSeleccionarCarpeta.TabIndex = 5
        Me.btnSeleccionarCarpeta.Text = "Seleccionar carpeta"
        Me.btnSeleccionarCarpeta.UseVisualStyleBackColor = True
        '
        'btnSiguiente
        '
        Me.btnSiguiente.AllowDrop = True
        Me.btnSiguiente.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.btnSiguiente.Font = New System.Drawing.Font("Arial Narrow", 14.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnSiguiente.Location = New System.Drawing.Point(265, 166)
        Me.btnSiguiente.Name = "btnSiguiente"
        Me.btnSiguiente.Size = New System.Drawing.Size(120, 35)
        Me.btnSiguiente.TabIndex = 6
        Me.btnSiguiente.Text = "Siguiente"
        Me.btnSiguiente.UseVisualStyleBackColor = True
        '
        'lblNombreActual
        '
        Me.lblNombreActual.AutoSize = True
        Me.lblNombreActual.Font = New System.Drawing.Font("Arial Narrow", 14.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblNombreActual.Location = New System.Drawing.Point(261, 12)
        Me.lblNombreActual.Name = "lblNombreActual"
        Me.lblNombreActual.Size = New System.Drawing.Size(171, 23)
        Me.lblNombreActual.TabIndex = 7
        Me.lblNombreActual.Text = "Nombre actual del PDF"
        '
        'lblContador
        '
        Me.lblContador.Controls.Add(Me.btnCerrarSesion)
        Me.lblContador.Controls.Add(Me.btnRotarIzquierda)
        Me.lblContador.Controls.Add(Me.btnRotarDerecha)
        Me.lblContador.Controls.Add(Me.lblPagina)
        Me.lblContador.Controls.Add(Me.nudPagina)
        Me.lblContador.Controls.Add(Me.lblRotacion)
        Me.lblContador.Controls.Add(Me.lblCaptura)
        Me.lblContador.Controls.Add(Me.btnEliminar)
        Me.lblContador.Controls.Add(Me.lblTotalPdf)
        Me.lblContador.Controls.Add(Me.lbl)
        Me.lblContador.Controls.Add(Me.Splitter1)
        Me.lblContador.Controls.Add(Me.Label1)
        Me.lblContador.Controls.Add(Me.btnDividir)
        Me.lblContador.Controls.Add(Me.txtBloques)
        Me.lblContador.Controls.Add(Me.rbRegla2)
        Me.lblContador.Controls.Add(Me.rbRegla1)
        Me.lblContador.Controls.Add(Me.btnDuplicar)
        Me.lblContador.Controls.Add(Me.btnSeleccionarCarpeta)
        Me.lblContador.Controls.Add(Me.btnSiguiente)
        Me.lblContador.Controls.Add(Me.lblEstado)
        Me.lblContador.Controls.Add(Me.txtNombre)
        Me.lblContador.Controls.Add(Me.lblNombreActual)
        Me.lblContador.Controls.Add(Me.btnRenombrar)
        Me.lblContador.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.lblContador.Location = New System.Drawing.Point(0, 319)
        Me.lblContador.Name = "lblContador"
        Me.lblContador.Size = New System.Drawing.Size(1349, 223)
        Me.lblContador.TabIndex = 9
        '
        'lblCaptura
        '
        Me.lblCaptura.AutoSize = True
        Me.lblCaptura.Font = New System.Drawing.Font("Arial Narrow", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblCaptura.Location = New System.Drawing.Point(584, 92)
        Me.lblCaptura.Name = "lblCaptura"
        Me.lblCaptura.Size = New System.Drawing.Size(112, 20)
        Me.lblCaptura.TabIndex = 23
        Me.lblCaptura.Text = "Primera captura"
        '
        'btnEliminar
        '
        Me.btnEliminar.BackColor = System.Drawing.SystemColors.ButtonFace
        Me.btnEliminar.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.btnEliminar.Font = New System.Drawing.Font("Arial Narrow", 14.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnEliminar.Location = New System.Drawing.Point(853, 116)
        Me.btnEliminar.Name = "btnEliminar"
        Me.btnEliminar.Size = New System.Drawing.Size(105, 35)
        Me.btnEliminar.TabIndex = 22
        Me.btnEliminar.Text = "Eliminar"
        Me.btnEliminar.UseVisualStyleBackColor = False
        '
        'lblTotalPdf
        '
        Me.lblTotalPdf.AutoSize = True
        Me.lblTotalPdf.Font = New System.Drawing.Font("Arial Narrow", 14.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblTotalPdf.Location = New System.Drawing.Point(261, 47)
        Me.lblTotalPdf.Name = "lblTotalPdf"
        Me.lblTotalPdf.Size = New System.Drawing.Size(100, 23)
        Me.lblTotalPdf.TabIndex = 21
        Me.lblTotalPdf.Text = "Total de PDF"
        '
        'lbl
        '
        Me.lbl.AutoSize = True
        Me.lbl.Font = New System.Drawing.Font("Arial Narrow", 14.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbl.Location = New System.Drawing.Point(772, 33)
        Me.lbl.Name = "lbl"
        Me.lbl.Size = New System.Drawing.Size(234, 23)
        Me.lbl.TabIndex = 20
        Me.lbl.Text = "Renombrados: 0 | Duplicados: 0"
        '
        'Splitter1
        '
        Me.Splitter1.Location = New System.Drawing.Point(0, 0)
        Me.Splitter1.Name = "Splitter1"
        Me.Splitter1.Size = New System.Drawing.Size(3, 223)
        Me.Splitter1.TabIndex = 19
        Me.Splitter1.TabStop = False
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Font = New System.Drawing.Font("Arial Narrow", 14.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.Location = New System.Drawing.Point(1047, 33)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(182, 23)
        Me.Label1.TabIndex = 18
        Me.Label1.Text = "SECCION PARA LIBROS"
        '
        'btnDividir
        '
        Me.btnDividir.AllowDrop = True
        Me.btnDividir.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.btnDividir.Font = New System.Drawing.Font("Arial Narrow", 14.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnDividir.Location = New System.Drawing.Point(1209, 127)
        Me.btnDividir.Name = "btnDividir"
        Me.btnDividir.Size = New System.Drawing.Size(128, 35)
        Me.btnDividir.TabIndex = 17
        Me.btnDividir.Text = "DIVIDIR"
        Me.btnDividir.UseVisualStyleBackColor = True
        '
        'txtBloques
        '
        Me.txtBloques.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtBloques.Font = New System.Drawing.Font("Arial Narrow", 15.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtBloques.Location = New System.Drawing.Point(1040, 80)
        Me.txtBloques.Name = "txtBloques"
        Me.txtBloques.Size = New System.Drawing.Size(297, 32)
        Me.txtBloques.TabIndex = 16
        '
        'rbRegla2
        '
        Me.rbRegla2.AutoSize = True
        Me.rbRegla2.Location = New System.Drawing.Point(23, 106)
        Me.rbRegla2.Name = "rbRegla2"
        Me.rbRegla2.Size = New System.Drawing.Size(62, 17)
        Me.rbRegla2.TabIndex = 13
        Me.rbRegla2.TabStop = True
        Me.rbRegla2.Text = "Regla 2"
        Me.rbRegla2.UseVisualStyleBackColor = True
        '
        'rbRegla1
        '
        Me.rbRegla1.AutoSize = True
        Me.rbRegla1.Location = New System.Drawing.Point(23, 67)
        Me.rbRegla1.Name = "rbRegla1"
        Me.rbRegla1.Size = New System.Drawing.Size(62, 17)
        Me.rbRegla1.TabIndex = 12
        Me.rbRegla1.TabStop = True
        Me.rbRegla1.Text = "Regla 1"
        Me.rbRegla1.UseVisualStyleBackColor = True
        '
        'btnDuplicar
        '
        Me.btnDuplicar.BackColor = System.Drawing.SystemColors.ButtonFace
        Me.btnDuplicar.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.btnDuplicar.Font = New System.Drawing.Font("Arial Narrow", 14.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnDuplicar.Location = New System.Drawing.Point(722, 116)
        Me.btnDuplicar.Name = "btnDuplicar"
        Me.btnDuplicar.Size = New System.Drawing.Size(105, 35)
        Me.btnDuplicar.TabIndex = 9
        Me.btnDuplicar.Text = "Duplicar"
        Me.btnDuplicar.UseVisualStyleBackColor = False
        '
        'WebView21
        '
        Me.WebView21.AllowExternalDrop = True
        Me.WebView21.CreationProperties = Nothing
        Me.WebView21.DefaultBackgroundColor = System.Drawing.Color.White
        Me.WebView21.Dock = System.Windows.Forms.DockStyle.Fill
        Me.WebView21.Location = New System.Drawing.Point(0, 0)
        Me.WebView21.Name = "WebView21"
        Me.WebView21.Size = New System.Drawing.Size(1349, 319)
        Me.WebView21.TabIndex = 10
        Me.WebView21.ZoomFactor = 1.0R
        '
        'lblRotacion
        '
        Me.lblRotacion.AutoSize = True
        Me.lblRotacion.Font = New System.Drawing.Font("Arial Narrow", 12.0!, System.Drawing.FontStyle.Bold)
        Me.lblRotacion.Location = New System.Drawing.Point(400, 170)
        Me.lblRotacion.Name = "lblRotacion"
        Me.lblRotacion.Size = New System.Drawing.Size(56, 20)
        Me.lblRotacion.TabIndex = 25
        Me.lblRotacion.Text = "Rotar:"
        '
        'lblPagina
        '
        Me.lblPagina.AutoSize = True
        Me.lblPagina.Font = New System.Drawing.Font("Arial Narrow", 12.0!)
        Me.lblPagina.Location = New System.Drawing.Point(460, 170)
        Me.lblPagina.Name = "lblPagina"
        Me.lblPagina.Size = New System.Drawing.Size(32, 20)
        Me.lblPagina.TabIndex = 26
        Me.lblPagina.Text = "Pág"
        '
        'nudPagina
        '
        Me.nudPagina.Font = New System.Drawing.Font("Arial Narrow", 12.0!)
        Me.nudPagina.Location = New System.Drawing.Point(496, 168)
        Me.nudPagina.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.nudPagina.Name = "nudPagina"
        Me.nudPagina.Size = New System.Drawing.Size(55, 26)
        Me.nudPagina.TabIndex = 27
        Me.nudPagina.Value = New Decimal(New Integer() {1, 0, 0, 0})
        '
        'btnRotarIzquierda
        '
        Me.btnRotarIzquierda.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.btnRotarIzquierda.Font = New System.Drawing.Font("Arial", 14.0!, System.Drawing.FontStyle.Bold)
        Me.btnRotarIzquierda.Location = New System.Drawing.Point(560, 164)
        Me.btnRotarIzquierda.Name = "btnRotarIzquierda"
        Me.btnRotarIzquierda.Size = New System.Drawing.Size(40, 35)
        Me.btnRotarIzquierda.TabIndex = 28
        Me.btnRotarIzquierda.Text = "↺"
        Me.btnRotarIzquierda.UseVisualStyleBackColor = True
        '
        'btnRotarDerecha
        '
        Me.btnRotarDerecha.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.btnRotarDerecha.Font = New System.Drawing.Font("Arial", 14.0!, System.Drawing.FontStyle.Bold)
        Me.btnRotarDerecha.Location = New System.Drawing.Point(610, 164)
        Me.btnRotarDerecha.Name = "btnRotarDerecha"
        Me.btnRotarDerecha.Size = New System.Drawing.Size(40, 35)
        Me.btnRotarDerecha.TabIndex = 29
        Me.btnRotarDerecha.Text = "↻"
        Me.btnRotarDerecha.UseVisualStyleBackColor = True
        '
        'btnCerrarSesion
        '
        Me.btnCerrarSesion.BackColor = System.Drawing.Color.IndianRed
        Me.btnCerrarSesion.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.btnCerrarSesion.Font = New System.Drawing.Font("Arial Narrow", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnCerrarSesion.ForeColor = System.Drawing.Color.White
        Me.btnCerrarSesion.Location = New System.Drawing.Point(1235, 12)
        Me.btnCerrarSesion.Name = "btnCerrarSesion"
        Me.btnCerrarSesion.Size = New System.Drawing.Size(100, 32)
        Me.btnCerrarSesion.TabIndex = 30
        Me.btnCerrarSesion.Text = "Cerrar sesión"
        Me.btnCerrarSesion.UseVisualStyleBackColor = False
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.SystemColors.ButtonFace
        Me.ClientSize = New System.Drawing.Size(1349, 542)
        Me.Controls.Add(Me.WebView21)
        Me.Controls.Add(Me.lblContador)
        Me.Name = "Form1"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Validación PDF"
        Me.WindowState = System.Windows.Forms.FormWindowState.Maximized
        Me.lblContador.ResumeLayout(False)
        Me.lblContador.PerformLayout()
        CType(Me.WebView21, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudPagina, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents btnRenombrar As Button
    Friend WithEvents lblEstado As Label
    Friend WithEvents txtNombre As TextBox
    Friend WithEvents btnSeleccionarCarpeta As Button
    Friend WithEvents lblNombreActual As Label
    Friend WithEvents lblContador As Panel
    Friend WithEvents WebView21 As Microsoft.Web.WebView2.WinForms.WebView2
    Friend WithEvents btnDuplicar As Button
    Friend WithEvents rbRegla2 As RadioButton
    Friend WithEvents rbRegla1 As RadioButton
    Friend WithEvents btnSiguiente As Button
    Friend WithEvents txtBloques As TextBox
    Friend WithEvents btnDividir As Button
    Friend WithEvents Label1 As Label
    Friend WithEvents Splitter1 As Splitter
    Friend WithEvents lbl As Label
    Friend WithEvents lblTotalPdf As Label
    Friend WithEvents btnEliminar As Button
    Friend WithEvents lblCaptura As Label
    Friend WithEvents lblRotacion As Label
    Friend WithEvents lblPagina As Label
    Friend WithEvents nudPagina As NumericUpDown
    Friend WithEvents btnRotarIzquierda As Button
    Friend WithEvents btnRotarDerecha As Button
    Friend WithEvents btnCerrarSesion As Button
End Class
