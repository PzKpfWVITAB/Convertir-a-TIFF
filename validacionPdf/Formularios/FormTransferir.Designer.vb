<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FormTransferir
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
        Me.btnTransferir = New System.Windows.Forms.Button()
        Me.btnCancelar = New System.Windows.Forms.Button()
        Me.btnConfigRecursos = New System.Windows.Forms.Button()
        Me.dgvBitacora = New System.Windows.Forms.DataGridView()
        Me.bgWorker = New System.ComponentModel.BackgroundWorker()
        Me.ListBoxOrigen = New System.Windows.Forms.ListBox()
        Me.ListViewDestino = New System.Windows.Forms.ListView()
        Me.LabelOrigenRuta = New System.Windows.Forms.Label()
        Me.ListViewOrigen = New System.Windows.Forms.ListView()
        Me.LabelDestinoRuta = New System.Windows.Forms.Label()
        Me.ListBoxDestino = New System.Windows.Forms.ListBox()
        Me.SplitContainer1 = New System.Windows.Forms.SplitContainer()
        Me.Label1 = New System.Windows.Forms.Label()
        CType(Me.dgvBitacora, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SplitContainer1.SuspendLayout()
        Me.SuspendLayout()
        '
        'btnTransferir
        '
        Me.btnTransferir.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!)
        Me.btnTransferir.Location = New System.Drawing.Point(571, 592)
        Me.btnTransferir.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.btnTransferir.Name = "btnTransferir"
        Me.btnTransferir.Size = New System.Drawing.Size(200, 63)
        Me.btnTransferir.TabIndex = 4
        Me.btnTransferir.Text = "Iniciar"
        Me.btnTransferir.UseVisualStyleBackColor = True
        '
        'btnCancelar
        '
        Me.btnCancelar.Enabled = False
        Me.btnCancelar.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold)
        Me.btnCancelar.ForeColor = System.Drawing.Color.DarkRed
        Me.btnCancelar.Location = New System.Drawing.Point(782, 592)
        Me.btnCancelar.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.btnCancelar.Name = "btnCancelar"
        Me.btnCancelar.Size = New System.Drawing.Size(200, 63)
        Me.btnCancelar.TabIndex = 15
        Me.btnCancelar.Text = "Cancelar"
        Me.btnCancelar.UseVisualStyleBackColor = True
        '
        'dgvBitacora
        '
        Me.dgvBitacora.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvBitacora.Location = New System.Drawing.Point(51, 661)
        Me.dgvBitacora.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.dgvBitacora.Name = "dgvBitacora"
        Me.dgvBitacora.RowHeadersWidth = 51
        Me.dgvBitacora.RowTemplate.Height = 24
        Me.dgvBitacora.Size = New System.Drawing.Size(931, 230)
        Me.dgvBitacora.TabIndex = 5
        '
        'bgWorker
        '
        Me.bgWorker.WorkerReportsProgress = True
        Me.bgWorker.WorkerSupportsCancellation = True
        '
        'ListBoxOrigen
        '
        Me.ListBoxOrigen.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ListBoxOrigen.FormattingEnabled = True
        Me.ListBoxOrigen.ItemHeight = 25
        Me.ListBoxOrigen.Location = New System.Drawing.Point(51, 58)
        Me.ListBoxOrigen.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.ListBoxOrigen.Name = "ListBoxOrigen"
        Me.ListBoxOrigen.Size = New System.Drawing.Size(495, 104)
        Me.ListBoxOrigen.TabIndex = 6
        '
        'ListViewDestino
        '
        Me.ListViewDestino.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!)
        Me.ListViewDestino.HideSelection = False
        Me.ListViewDestino.Location = New System.Drawing.Point(571, 228)
        Me.ListViewDestino.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.ListViewDestino.Name = "ListViewDestino"
        Me.ListViewDestino.Size = New System.Drawing.Size(409, 358)
        Me.ListViewDestino.TabIndex = 7
        Me.ListViewDestino.UseCompatibleStateImageBehavior = False
        '
        'LabelOrigenRuta
        '
        Me.LabelOrigenRuta.AutoSize = True
        Me.LabelOrigenRuta.Location = New System.Drawing.Point(48, 196)
        Me.LabelOrigenRuta.Name = "LabelOrigenRuta"
        Me.LabelOrigenRuta.Size = New System.Drawing.Size(48, 16)
        Me.LabelOrigenRuta.TabIndex = 8
        Me.LabelOrigenRuta.Text = "Label1"
        '
        'ListViewOrigen
        '
        Me.ListViewOrigen.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!)
        Me.ListViewOrigen.HideSelection = False
        Me.ListViewOrigen.Location = New System.Drawing.Point(51, 228)
        Me.ListViewOrigen.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.ListViewOrigen.Name = "ListViewOrigen"
        Me.ListViewOrigen.Size = New System.Drawing.Size(495, 427)
        Me.ListViewOrigen.TabIndex = 9
        Me.ListViewOrigen.UseCompatibleStateImageBehavior = False
        '
        'LabelDestinoRuta
        '
        Me.LabelDestinoRuta.AutoSize = True
        Me.LabelDestinoRuta.Location = New System.Drawing.Point(568, 196)
        Me.LabelDestinoRuta.Name = "LabelDestinoRuta"
        Me.LabelDestinoRuta.Size = New System.Drawing.Size(48, 16)
        Me.LabelDestinoRuta.TabIndex = 10
        Me.LabelDestinoRuta.Text = "Label1"
        '
        'ListBoxDestino
        '
        Me.ListBoxDestino.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!)
        Me.ListBoxDestino.FormattingEnabled = True
        Me.ListBoxDestino.ItemHeight = 25
        Me.ListBoxDestino.Location = New System.Drawing.Point(571, 58)
        Me.ListBoxDestino.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.ListBoxDestino.Name = "ListBoxDestino"
        Me.ListBoxDestino.Size = New System.Drawing.Size(409, 104)
        Me.ListBoxDestino.TabIndex = 11
        '
        'SplitContainer1
        '
        Me.SplitContainer1.Location = New System.Drawing.Point(69, 475)
        Me.SplitContainer1.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.SplitContainer1.Name = "SplitContainer1"
        '
        'SplitContainer1.Panel2
        '
        Me.SplitContainer1.Panel2.Enabled = False
        Me.SplitContainer1.Size = New System.Drawing.Size(149, 100)
        Me.SplitContainer1.SplitterDistance = 49
        Me.SplitContainer1.TabIndex = 12
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!)
        Me.Label1.Location = New System.Drawing.Point(51, 20)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(553, 31)
        Me.Label1.TabIndex = 13
        Me.Label1.Text = "TRANSFERENCIA CON MISMA ESTRUCTURA"
        '
        'btnConfigRecursos
        '
        Me.btnConfigRecursos.Font = New System.Drawing.Font("Microsoft Sans Serif", 10.0!)
        Me.btnConfigRecursos.Location = New System.Drawing.Point(740, 15)
        Me.btnConfigRecursos.Name = "btnConfigRecursos"
        Me.btnConfigRecursos.Size = New System.Drawing.Size(242, 38)
        Me.btnConfigRecursos.TabIndex = 14
        Me.btnConfigRecursos.Text = "⚙ Rendimiento (CPU/RAM)"
        Me.btnConfigRecursos.UseVisualStyleBackColor = True
        '
        'FormTransferir
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1084, 926)
        Me.Controls.Add(Me.btnConfigRecursos)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.dgvBitacora)
        Me.Controls.Add(Me.ListViewOrigen)
        Me.Controls.Add(Me.SplitContainer1)
        Me.Controls.Add(Me.ListBoxDestino)
        Me.Controls.Add(Me.LabelDestinoRuta)
        Me.Controls.Add(Me.LabelOrigenRuta)
        Me.Controls.Add(Me.ListViewDestino)
        Me.Controls.Add(Me.ListBoxOrigen)
        Me.Controls.Add(Me.btnCancelar)
        Me.Controls.Add(Me.btnTransferir)
        Me.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.Name = "FormTransferir"
        Me.Text = "TRANSFERENCIA DE SELLOS NEW"
        CType(Me.dgvBitacora, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.SplitContainer1.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents btnTransferir As Button
    Friend WithEvents btnCancelar As Button
    Friend WithEvents dgvBitacora As DataGridView
    Friend WithEvents bgWorker As System.ComponentModel.BackgroundWorker
    Friend WithEvents ListBoxOrigen As ListBox
    Friend WithEvents ListViewDestino As ListView
    Friend WithEvents LabelOrigenRuta As Label
    Friend WithEvents ListViewOrigen As ListView
    Friend WithEvents LabelDestinoRuta As Label
    Friend WithEvents ListBoxDestino As ListBox
    Friend WithEvents SplitContainer1 As SplitContainer
    Friend WithEvents Label1 As Label
    Friend WithEvents btnConfigRecursos As Button
End Class
