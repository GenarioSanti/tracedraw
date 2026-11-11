Attribute VB_Name = "BannerCut"
Option Explicit
Private arteLimite As ShapeRange

Private Sub ReunirArteLimite(shapes As Shapes)
    Dim item As Shape
    For Each item In shapes
        If InStr(item.Name, "RegMark") = 0 And InStr(item.Name, "BC_MARK_") = 0 Then
            If item.Type = cdrGroupShape Then
                ReunirArteLimite item.Shapes
            Else
                arteLimite.Add item
            End If
        End If
    Next item
End Sub

Public Sub Limite()
    Dim selecionados As ShapeRange
    Dim item As Shape
    Dim resultado As Shape
    Dim proximo As Shape
    Dim unidade As Long
    Dim detalhe As String
    unidade = ActiveDocument.Unit
    On Error GoTo FalhaLimite
    Set selecionados = ActiveSelectionRange
    Set arteLimite = Application.CreateShapeRange()
    ReunirArteLimite selecionados.Shapes
    If arteLimite.Count = 0 Then
        MsgBox "Selecione a arte para criar o contorno. As marcas serao ignoradas.", vbExclamation, "BannerCut"
        Exit Sub
    End If
    ActiveDocument.Unit = cdrMillimeter
    Set item = arteLimite.Item(1)
    Set resultado = item.Weld(item, True, True)
    If resultado Is Nothing Then
        MsgBox "Nao foi possivel obter um contorno fechado dessa arte.", vbExclamation, "BannerCut"
        ActiveDocument.Unit = unidade
        Exit Sub
    End If
    For Each item In arteLimite.Shapes
        Set proximo = item.Weld(resultado, True, True)
        If proximo Is Nothing Then
            resultado.Delete
            ActiveDocument.Unit = unidade
            MsgBox "Nao foi possivel unir os contornos. Os originais foram preservados.", vbExclamation, "BannerCut"
            Exit Sub
        End If
        resultado.Delete
        Set resultado = proximo
    Next item
    resultado.Name = "BannerCut - Contorno de corte"
    resultado.Fill.ApplyNoFill
    resultado.Outline.Width = 0.2
    resultado.Outline.Color.RGBAssign 0, 0, 0
    resultado.CreateSelection
    ActiveDocument.Unit = unidade
    Exit Sub
FalhaLimite:
    detalhe = Err.Description
    On Error Resume Next
    If Not resultado Is Nothing Then resultado.Delete
    ActiveDocument.Unit = unidade
    MsgBox "Nao foi possivel criar o contorno: " & detalhe, vbExclamation, "BannerCut"
End Sub

Public Sub Enviar()
    Dim sr As ShapeRange
    Dim sh As Shape
    Dim nomes() As String
    Dim i As Long
    Dim n As Long
    Dim erro As String
    Dim marcas As Long
    Set sr = ActiveSelectionRange
    n = sr.Count
    If n < 2 Then
        MsgBox "Selecione os contornos e as marcas juntos.", vbExclamation, "BannerCut"
        Exit Sub
    End If
    ReDim nomes(1 To n)
    For i = 1 To n
        nomes(i) = sr.Item(i).Name
        If nomes(i) = "RegMark X00 Y00" Then marcas = marcas + 1
    Next i
    If marcas <> 1 Then
        MsgBox "Selecione exatamente um conjunto RegMark X00 Y00.", vbExclamation, "BannerCut"
        Exit Sub
    End If
    On Error GoTo Falha
    For i = 1 To n
        Set sh = sr.Item(i)
        If nomes(i) = "RegMark X00 Y00" Then
            sh.Name = "BC_MARK_X00_Y00"
        Else
            sh.Name = "BC_SELECTED_" & CStr(i)
        End If
    Next i
    ActiveDocument.Export "__MAILBOX__\enviar.svg", cdrSVG
Restaurar:
    For i = 1 To n
        sr.Item(i).Name = nomes(i)
    Next i
    If erro <> "" Then
        MsgBox erro, vbExclamation, "BannerCut"
    End If
    Exit Sub
Falha:
    erro = "Nao foi possivel preparar o desenho: " & Err.Description
    Resume Restaurar
End Sub

Public Sub Marcas()
    Dim sr As ShapeRange
    Dim sh As Shape
    Dim cv As Curve
    Dim sp As SubPath
    Dim mk As Shape
    Dim x As Double, y As Double, w As Double, h As Double
    Dim l As Double, r As Double, b As Double, t As Double
    Dim unidade As Long
    Set sr = ActiveSelectionRange
    If sr.Count = 0 Then
        MsgBox "Selecione os contornos que receberao as marcas.", vbExclamation, "BannerCut"
        Exit Sub
    End If
    For Each sh In ActivePage.Shapes
        If InStr(sh.Name, "RegMark") > 0 Then
            MsgBox "A pagina ja tem marcas. Ajuste as existentes ou remova-as para gerar novamente.", vbExclamation, "BannerCut"
            Exit Sub
        End If
    Next sh
    unidade = ActiveDocument.Unit
    On Error GoTo FalhaMarcas
    ActiveDocument.Unit = cdrMillimeter
    x = sr.LeftX
    y = sr.BottomY
    w = sr.SizeWidth
    h = sr.SizeHeight
    If w <= 0 Or h <= 0 Then
        ActiveDocument.Unit = unidade
        MsgBox "A selecao precisa ter largura e altura maiores que zero.", vbExclamation, "BannerCut"
        Exit Sub
    End If
    l = x - 14
    r = x + w + 14
    b = y - 14
    t = y + h + 14
    Set cv = ActiveDocument.CreateCurve
    Set sp = cv.CreateSubPath(l + 20, t)
    sp.AppendLineSegment l + 17, t
    Set sp = cv.CreateSubPath(r - 14, b)
    sp.AppendLineSegment r, b
    sp.AppendLineSegment r, b + 14
    Set sp = cv.CreateSubPath(r - 14, t)
    sp.AppendLineSegment r, t
    sp.AppendLineSegment r, t - 14
    Set sp = cv.CreateSubPath(l + 14, t)
    sp.AppendLineSegment l, t
    sp.AppendLineSegment l, t - 14
    Set sp = cv.CreateSubPath(l + 14, b)
    sp.AppendLineSegment l, b
    sp.AppendLineSegment l, b + 14
    Set mk = ActiveLayer.CreateCurve(cv)
    mk.Name = "RegMark X00 Y00"
    mk.Fill.ApplyNoFill
    mk.Outline.Width = 0.5
    mk.Outline.Color.RGBAssign 0, 0, 0
    mk.CreateSelection
    ActiveDocument.Unit = unidade
    Exit Sub
FalhaMarcas:
    ActiveDocument.Unit = unidade
    MsgBox "Nao foi possivel criar as marcas: " & Err.Description, vbExclamation, "BannerCut"
End Sub

Public Sub Configurar()
    On Error GoTo Falha
    ActiveDocument.Export "__MAILBOX__\configurar.svg", cdrSVG
    Exit Sub
Falha:
    MsgBox "Nao foi possivel abrir as configuracoes: " & Err.Description, vbExclamation, "BannerCut"
End Sub
