namespace DeviceManager.Server.Components.Pages;

using DeviceManager.Server.Components.Dialogs;
using DeviceManager.Server.Infrastructure.Components;
using DeviceManager.Server.Models.Forms;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class FunctionsPage
{
    private List<FunctionEntity> functions = [];

    [Inject]
    public required FunctionService FunctionService { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    protected override Task OnInitializedAsync() =>
        ReloadAsync();

    private async Task ReloadAsync()
    {
        functions = await FunctionService.QueryAllAsync();
    }

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    private async Task AddAsync()
    {
        var form = await ShowEditDialog("Function追加", new FunctionForm { Json = "{\r\n}" }, isNew: true);
        if (form is null)
        {
            return;
        }

        await FunctionService.SetAsync(form.Name, form.Json);
        Snackbar.AddSuccess("保存しました。");
        await ReloadAsync();
    }

    private async Task EditAsync(FunctionEntity entity)
    {
        var form = await ShowEditDialog("Function編集", new FunctionForm { Name = entity.Name, Json = entity.Json }, isNew: false);
        if (form is null)
        {
            return;
        }

        await FunctionService.SetAsync(form.Name, form.Json);
        Snackbar.AddSuccess("保存しました。");
        await ReloadAsync();
    }

    private async Task DeleteAsync(FunctionEntity entity)
    {
        if (!await DialogService.ShowConfirm("Function削除", $"{entity.Name} を削除してよろしいですか？"))
        {
            return;
        }

        if (await FunctionService.DeleteAsync(entity.Name))
        {
            Snackbar.AddSuccess("削除しました。");
        }
        else
        {
            Snackbar.AddError("対象が存在しません。");
        }

        await ReloadAsync();
    }

    private async Task<FunctionForm?> ShowEditDialog(string title, FunctionForm form, bool isNew)
    {
        var reference = await DialogService.ShowAsync<FunctionEditDialog>(
            string.Empty,
            new DialogParameters
            {
                { nameof(FunctionEditDialog.Title), title },
                { nameof(FunctionEditDialog.Form), form },
                { nameof(FunctionEditDialog.IsNew), isNew }
            },
            new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true });
        var result = await reference.Result;
        return (result is { Canceled: false }) ? (FunctionForm)result.Data! : null;
    }
}
