using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using TaxInvoiceExtractor.Logging;
using TaxInvoiceExtractor.Models;
using TaxInvoiceExtractor.Pdf;
using TaxInvoiceExtractor.Services;
using TaxInvoiceExtractor.Utils;

namespace TaxInvoiceExtractor.Mac;

public partial class MainWindow : Window
{
    public ObservableCollection<SelectedPdfItem> Files { get; } = [];
    public ObservableCollection<TaxInvoiceData> Results { get; } = [];

    private readonly ExtractionService _extractionService;
    private readonly ExcelExportService _excelService = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        _extractionService = new ExtractionService(
            new PdfTextExtractor(),
            new TaxInvoiceParser(new FieldExtractor()));
    }

    private async void OnChooseFolderClick(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "전자세금계산서 PDF가 들어 있는 폴더를 선택하세요.",
            AllowMultiple = false
        });
        if (folders.Count == 0) return;

        var folderPath = folders[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            await ShowMessageAsync("폴더 읽기 실패", "로컬 폴더 경로를 확인하지 못했습니다.");
            return;
        }

        try
        {
            var pdfFiles = PdfFolderService.GetPdfFiles(folderPath);
            if (pdfFiles.Count == 0)
            {
                await ShowMessageAsync("PDF 없음", "선택한 폴더에 PDF 파일이 없습니다.");
                return;
            }

            Files.Clear();
            AddFiles(pdfFiles);
            StatusLabel.Text = $"'{folderPath}' 폴더에서 PDF {pdfFiles.Count}개를 불러왔습니다.";
        }
        catch (Exception ex)
        {
            AppLogger.Error($"PDF 폴더 읽기 실패: {folderPath}", ex);
            await ShowMessageAsync("폴더 읽기 실패", $"선택한 폴더를 읽지 못했습니다.\n\n{ex.Message}");
        }
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        var valid = paths
            .Where(path => string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(path => Files.All(file => !string.Equals(file.FullPath, path, StringComparison.OrdinalIgnoreCase)));

        foreach (var path in valid)
            Files.Add(new SelectedPdfItem { FullPath = path, FileName = Path.GetFileName(path), Sequence = Files.Count + 1 });
        ResetResults();
        StatusLabel.Text = $"PDF {Files.Count}개가 선택되었습니다.";
    }

    private void OnMoveUpClick(object? sender, RoutedEventArgs e) => MoveSelected(-1);
    private void OnMoveDownClick(object? sender, RoutedEventArgs e) => MoveSelected(1);

    private void MoveSelected(int direction)
    {
        if (FileGrid.SelectedItem is not SelectedPdfItem item) return;
        var index = Files.IndexOf(item);
        var target = index + direction;
        if (target < 0 || target >= Files.Count) return;
        Files.Move(index, target);
        Renumber();
        FileGrid.SelectedItem = item;
        FileGrid.ScrollIntoView(item, null);
        ResetResults();
    }

    private void OnRemoveSelectedClick(object? sender, RoutedEventArgs e)
    {
        if (FileGrid.SelectedItem is not SelectedPdfItem item) return;
        Files.Remove(item);
        Renumber();
        ResetResults();
    }

    private void OnClearFilesClick(object? sender, RoutedEventArgs e)
    {
        Files.Clear();
        ResetResults();
        StatusLabel.Text = "PDF가 들어 있는 폴더를 선택하세요.";
    }

    private void Renumber()
    {
        for (var index = 0; index < Files.Count; index++) Files[index].Sequence = index + 1;
    }

    private void ResetResults()
    {
        Results.Clear();
        foreach (var file in Files) file.Status = "대기";
        SaveButton.IsEnabled = false;
    }

    private async void OnExtractClick(object? sender, RoutedEventArgs e)
    {
        if (Files.Count == 0)
        {
            await ShowMessageAsync("알림", "PDF 파일을 먼저 선택해주세요.");
            return;
        }

        SetBusy(true);
        Results.Clear();
        var progress = new Progress<ExtractionProgress>(item =>
        {
            ProgressBar.Maximum = item.Total;
            ProgressBar.Value = item.Current;
            ProgressLabel.Text = $"데이터 추출, 변환중... {item.Current} / {item.Total} | {item.FileName}";
        });

        try
        {
            var rows = await _extractionService.ExtractAsync(Files.ToList(), progress);
            foreach (var row in rows) Results.Add(row);
            SaveButton.IsEnabled = Results.Count > 0;
            var reviewCount = rows.Count(row => row.ExtractionStatus != "완료");
            StatusLabel.Text = reviewCount == 0
                ? $"추출이 완료되었습니다. {Results.Count}개 결과를 확인한 뒤 Excel로 저장하세요."
                : $"추출 완료: {Results.Count}개 중 {reviewCount}개는 확인 및 수정이 필요합니다.";
            var firstError = rows.FirstOrDefault(row => !string.IsNullOrWhiteSpace(row.ErrorMessage));
            if (firstError is not null)
                StatusLabel.Text += $" 첫 확인 항목: {firstError.SourceFileName} — {firstError.ErrorMessage}";
        }
        catch (Exception ex)
        {
            AppLogger.Error("전체 추출 작업 오류", ex);
            await ShowMessageAsync("오류", $"데이터 추출 중 오류가 발생했습니다.\n\n{ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        AddButton.IsEnabled = !busy;
        ExtractButton.IsEnabled = !busy;
        FileGrid.IsEnabled = !busy;
        ResultGrid.IsEnabled = !busy;
        ProgressBar.IsVisible = busy;
        ProgressLabel.IsVisible = busy;
        if (busy)
        {
            ProgressBar.Value = 0;
            ProgressLabel.Text = "데이터 추출, 변환중...";
        }
    }

    private async void OnSaveExcelClick(object? sender, RoutedEventArgs e)
    {
        var invalid = Results.Where(row => Validator.Validate(row).Count > 0).ToList();
        if (invalid.Count > 0)
        {
            var proceed = await ShowQuestionAsync("검증 확인",
                $"{invalid.Count}개 행에 비어 있거나 형식이 맞지 않는 값이 있습니다. 빈 셀을 유지한 채 저장할까요?");
            if (!proceed) return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Excel 파일로 저장",
            SuggestedFileName = $"전자세금계산서_추출결과_{DateTime.Now:yyyyMMdd}.xlsx",
            DefaultExtension = "xlsx",
            ShowOverwritePrompt = true,
            FileTypeChoices =
            [
                new FilePickerFileType("Excel 통합 문서")
                {
                    Patterns = ["*.xlsx"],
                    MimeTypes = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"]
                }
            ]
        });
        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            _excelService.Export(path, Results.ToList());
            AppLogger.Info($"Excel 저장 성공: {Path.GetFileName(path)}");
            StatusLabel.Text = $"Excel 저장 완료: {path}";
            await ShowMessageAsync("저장 완료", "Excel 파일을 저장했습니다. Numbers와 Mac용 Excel에서 열 수 있습니다.");
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Excel 저장 실패: {Path.GetFileName(path)}", ex);
            await ShowMessageAsync("저장 실패",
                $"Excel 파일을 저장하지 못했습니다. 파일이 열려 있거나 저장 권한이 없는지 확인해주세요.\n\n{ex.Message}");
        }
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = CreateDialog(title, message);
        var closeButton = new Button { Content = "확인", MinWidth = 88, HorizontalAlignment = HorizontalAlignment.Right };
        closeButton.Click += (_, _) => dialog.Close();
        ((StackPanel)dialog.Content!).Children.Add(closeButton);
        await dialog.ShowDialog(this);
    }

    private async Task<bool> ShowQuestionAsync(string title, string message)
    {
        var dialog = CreateDialog(title, message);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        var cancelButton = new Button { Content = "취소", MinWidth = 88 };
        var confirmButton = new Button { Content = "계속 저장", MinWidth = 88 };
        cancelButton.Click += (_, _) => dialog.Close(false);
        confirmButton.Click += (_, _) => dialog.Close(true);
        buttons.Children.Add(cancelButton);
        buttons.Children.Add(confirmButton);
        ((StackPanel)dialog.Content!).Children.Add(buttons);
        return await dialog.ShowDialog<bool>(this);
    }

    private static Window CreateDialog(string title, string message) => new()
    {
        Title = title,
        Width = 440,
        SizeToContent = SizeToContent.Height,
        CanResize = false,
        ShowInTaskbar = false,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        Content = new StackPanel
        {
            Margin = new Thickness(22),
            Spacing = 18,
            Children = { new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap } }
        }
    };
}
