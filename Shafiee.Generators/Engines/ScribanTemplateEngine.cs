namespace Shafiee.Generators.Engines;

using Scriban;

public class ScribanTemplateEngine
{
    public string Render(string templateContent, object model)
    {
        var template = Template.Parse(templateContent);
        if (template.HasErrors)
        {
            var errors = string.Join("\n", template.Messages);
            throw new InvalidOperationException($"خطا در پارس کردن قالب Scriban:\n{errors}");
        }

        return template.Render(model, member => member.Name);
    }
}