<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FormLotes
    Inherits System.Windows.Forms.Form

    'Form reemplaza a Dispose para limpiar la lista de componentes.
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

    'Requerido por el Diseñador de Windows Forms
    Private components As System.ComponentModel.IContainer

    'NOTA: el Diseñador de Windows Forms necesita el siguiente procedimiento
    'Se puede modificar usando el Diseñador de Windows Forms.  
    'No lo modifique con el editor de código.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.ListBox1 = New System.Windows.Forms.ListBox()
        Me.ListViewCarpetas = New System.Windows.Forms.ListView()
        Me.Nombre = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.Tipo = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.Tamano = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ListViewArchivos = New System.Windows.Forms.ListView()
        Me.ImageList1 = New System.Windows.Forms.ImageList(Me.components)
        Me.lblResumen = New System.Windows.Forms.Label()
        Me.btnProcesar = New System.Windows.Forms.Button()
        Me.btnCancelarLotes = New System.Windows.Forms.Button()
        Me.btnConfigRecursosLotes = New System.Windows.Forms.Button()
        Me.WebViewPDF = New Microsoft.Web.WebView2.WinForms.WebView2()
        Me.LabelRuta = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        CType(Me.WebViewPDF, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'ListBox1
        '
        Me.ListBox1.Font = New System.Drawing.Font("Microsoft Sans Serif", 14.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ListBox1.FormattingEnabled = True
        Me.ListBox1.ItemHeight = 29
        Me.ListBox1.Location = New System.Drawing.Point(16, 15)
        Me.ListBox1.Margin = New System.Windows.Forms.Padding(4)
        Me.ListBox1.Name = "ListBox1"
        Me.ListBox1.Size = New System.Drawing.Size(388, 120)
        Me.ListBox1.TabIndex = 0
        '
        'ListViewCarpetas
        '
        Me.ListViewCarpetas.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.Nombre, Me.Tipo, Me.Tamano})
        Me.ListViewCarpetas.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ListViewCarpetas.FullRowSelect = True
        Me.ListViewCarpetas.GridLines = True
        Me.ListViewCarpetas.HideSelection = False
        Me.ListViewCarpetas.Location = New System.Drawing.Point(16, 155)
        Me.ListViewCarpetas.Margin = New System.Windows.Forms.Padding(4)
        Me.ListViewCarpetas.Name = "ListViewCarpetas"
        Me.ListViewCarpetas.Size = New System.Drawing.Size(388, 738)
        Me.ListViewCarpetas.TabIndex = 1
        Me.ListViewCarpetas.UseCompatibleStateImageBehavior = False
        Me.ListViewCarpetas.View = System.Windows.Forms.View.Details
        '
        'Nombre
        '
        Me.Nombre.Text = "Nombre"
        Me.Nombre.Width = 177
        '
        'Tipo
        '
        Me.Tipo.Tag = ""
        Me.Tipo.Text = "Tipo"
        Me.Tipo.Width = 70
        '
        'Tamano
        '
        Me.Tamano.Text = "Tamaño"
        Me.Tamano.Width = 117
        '
        'ListViewArchivos
        '
        Me.ListViewArchivos.HideSelection = False
        Me.ListViewArchivos.LargeImageList = Me.ImageList1
        Me.ListViewArchivos.Location = New System.Drawing.Point(428, 285)
        Me.ListViewArchivos.Margin = New System.Windows.Forms.Padding(4)
        Me.ListViewArchivos.Name = "ListViewArchivos"
        Me.ListViewArchivos.Size = New System.Drawing.Size(473, 608)
        Me.ListViewArchivos.TabIndex = 2
        Me.ListViewArchivos.UseCompatibleStateImageBehavior = False
        Me.ListViewArchivos.View = System.Windows.Forms.View.Details
        '
        'ImageList1
        '
        Me.ImageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit
        Me.ImageList1.ImageSize = New System.Drawing.Size(32, 32)
        Me.ImageList1.TransparentColor = System.Drawing.Color.Transparent
        '
        'lblResumen
        '
        Me.lblResumen.AutoSize = True
        Me.lblResumen.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblResumen.Location = New System.Drawing.Point(423, 155)
        Me.lblResumen.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblResumen.Name = "lblResumen"
        Me.lblResumen.Size = New System.Drawing.Size(82, 25)
        Me.lblResumen.TabIndex = 3
        Me.lblResumen.Text = "Detalles"
        '
        'btnProcesar
        '
        Me.btnProcesar.BackColor = System.Drawing.SystemColors.Highlight
        Me.btnProcesar.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnProcesar.ForeColor = System.Drawing.SystemColors.ButtonHighlight
        Me.btnProcesar.Location = New System.Drawing.Point(428, 223)
        Me.btnProcesar.Margin = New System.Windows.Forms.Padding(4)
        Me.btnProcesar.Name = "btnProcesar"
        Me.btnProcesar.Size = New System.Drawing.Size(200, 54)
        Me.btnProcesar.TabIndex = 4
        Me.btnProcesar.Text = "Procesar"
        Me.btnProcesar.UseVisualStyleBackColor = False
        '
        'btnCancelarLotes
        '
        Me.btnCancelarLotes.Enabled = False
        Me.btnCancelarLotes.Font = New System.Drawing.Font("Microsoft Sans Serif", 10.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnCancelarLotes.ForeColor = System.Drawing.Color.DarkRed
        Me.btnCancelarLotes.Location = New System.Drawing.Point(636, 223)
        Me.btnCancelarLotes.Margin = New System.Windows.Forms.Padding(4)
        Me.btnCancelarLotes.Name = "btnCancelarLotes"
        Me.btnCancelarLotes.Size = New System.Drawing.Size(125, 54)
        Me.btnCancelarLotes.TabIndex = 6
        Me.btnCancelarLotes.Text = "Cancelar"
        Me.btnCancelarLotes.UseVisualStyleBackColor = True
        '
        'btnConfigRecursosLotes
        '
        Me.btnConfigRecursosLotes.Font = New System.Drawing.Font("Microsoft Sans Serif", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnConfigRecursosLotes.Location = New System.Drawing.Point(769, 223)
        Me.btnConfigRecursosLotes.Margin = New System.Windows.Forms.Padding(4)
        Me.btnConfigRecursosLotes.Name = "btnConfigRecursosLotes"
        Me.btnConfigRecursosLotes.Size = New System.Drawing.Size(132, 54)
        Me.btnConfigRecursosLotes.TabIndex = 5
        Me.btnConfigRecursosLotes.Text = "⚙ Recursos"
        Me.btnConfigRecursosLotes.UseVisualStyleBackColor = True
        '
        'WebViewPDF
        '
        Me.WebViewPDF.AllowExternalDrop = True
        Me.WebViewPDF.BackColor = System.Drawing.SystemColors.ControlLight
        Me.WebViewPDF.CreationProperties = Nothing
        Me.WebViewPDF.DefaultBackgroundColor = System.Drawing.Color.White
        Me.WebViewPDF.Location = New System.Drawing.Point(932, 189)
        Me.WebViewPDF.Name = "WebViewPDF"
        Me.WebViewPDF.Size = New System.Drawing.Size(1062, 704)
        Me.WebViewPDF.TabIndex = 6
        Me.WebViewPDF.ZoomFactor = 1.0R
        '
        'LabelRuta
        '
        Me.LabelRuta.AutoSize = True
        Me.LabelRuta.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LabelRuta.Location = New System.Drawing.Point(423, 189)
        Me.LabelRuta.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.LabelRuta.Name = "LabelRuta"
        Me.LabelRuta.Size = New System.Drawing.Size(52, 25)
        Me.LabelRuta.TabIndex = 7
        Me.LabelRuta.Text = "Ruta"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Font = New System.Drawing.Font("Microsoft Sans Serif", 20.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.Location = New System.Drawing.Point(421, 15)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(355, 39)
        Me.Label1.TabIndex = 8
        Me.Label1.Text = "Sistema de Validación"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.Location = New System.Drawing.Point(927, 155)
        Me.Label2.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(186, 25)
        Me.Label2.TabIndex = 9
        Me.Label2.Text = "Visor de documento"
        '
        'FormLotes
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(2020, 937)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.LabelRuta)
        Me.Controls.Add(Me.WebViewPDF)
        Me.Controls.Add(Me.btnCancelarLotes)
        Me.Controls.Add(Me.btnConfigRecursosLotes)
        Me.Controls.Add(Me.btnProcesar)
        Me.Controls.Add(Me.lblResumen)
        Me.Controls.Add(Me.ListViewArchivos)
        Me.Controls.Add(Me.ListViewCarpetas)
        Me.Controls.Add(Me.ListBox1)
        Me.Margin = New System.Windows.Forms.Padding(4)
        Me.Name = "FormLotes"
        Me.Text = "FormLotes"
        CType(Me.WebViewPDF, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents ListBox1 As ListBox
    Friend WithEvents ListViewCarpetas As ListView
    Friend WithEvents Nombre As ColumnHeader
    Friend WithEvents ListViewArchivos As ListView
    Friend WithEvents Tipo As ColumnHeader
    Friend WithEvents Tamano As ColumnHeader
    Friend WithEvents lblResumen As Label
    Friend WithEvents btnProcesar As Button
    Friend WithEvents btnCancelarLotes As Button
    Friend WithEvents btnConfigRecursosLotes As Button
    Friend WithEvents ImageList1 As ImageList
    Friend WithEvents WebViewPDF As Microsoft.Web.WebView2.WinForms.WebView2
    Friend WithEvents LabelRuta As Label
    Friend WithEvents Label1 As Label
    Friend WithEvents Label2 As Label
End Class
