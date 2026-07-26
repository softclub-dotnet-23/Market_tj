namespace MarketTJ.Application.Interfaces.Services;

// Реализация — в WebApi (MarketTJ.WebApi/Services/LocalFileStorageService),
// т.к. физическое расположение файлов завязано на IWebHostEnvironment —
// тот же принцип, что и у ICurrentUserService: ASP.NET Core-specific
// концепция не должна протекать в Application-слой напрямую.
public interface IFileStorageService
{
    // subFolder — относительный путь внутри wwwroot/uploads (например
    // "avatars/3" или "listings/12"). Возвращает публичный относительный URL
    // сохранённого файла (например "/uploads/avatars/3/a1b2c3.jpg"), который
    // уже отдаётся статикой (app.UseStaticFiles() в Program.cs).
    Task<string> SaveAsync(Stream content, string fileName, string subFolder);

    // relativeUrl — то, что вернул SaveAsync. Молча ничего не делает, если
    // файла нет (значение могло быть null или файл уже удалён вручную).
    void Delete(string? relativeUrl);
}
