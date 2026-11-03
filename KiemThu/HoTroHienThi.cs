using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

// Bộ dựng HTML phía máy chủ không dùng trình duyệt, không chạy JavaScript hay mô phỏng thao tác người dùng.
public sealed class DieuHuongKiemThu : NavigationManager
{
    public DieuHuongKiemThu() => Initialize("http://localhost/", "http://localhost/");
    protected override void NavigateToCore(string uri, bool forceLoad) => Uri = ToAbsoluteUri(uri).ToString();
}

public sealed class JavaScriptKhongTuongTac : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => ValueTask.FromException<TValue>(new InvalidOperationException("Kiểm thử HTML không được gọi JavaScript: " + identifier));
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken token, object?[]? args)
        => InvokeAsync<TValue>(identifier, args);
}

