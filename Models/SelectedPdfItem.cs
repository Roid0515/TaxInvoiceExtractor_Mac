using System.ComponentModel;

namespace TaxInvoiceExtractor.Models;

public sealed class SelectedPdfItem : INotifyPropertyChanged
{
    private int _sequence;
    private string _fileName = string.Empty;
    private string _status = "대기";
    private string _fullPath = string.Empty;

    public int Sequence { get => _sequence; set => Set(ref _sequence, value); }
    public string FileName { get => _fileName; set => Set(ref _fileName, value ?? string.Empty); }
    public string Status { get => _status; set => Set(ref _status, value ?? string.Empty); }
    public string FullPath { get => _fullPath; set => Set(ref _fullPath, value ?? string.Empty); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
