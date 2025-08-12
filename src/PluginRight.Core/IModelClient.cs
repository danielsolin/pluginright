#nullable enable

using System.Threading.Tasks;

namespace PluginRight.Core;

public interface IModelClient
{
    Task<string> GenerateLogicAsync(Spec spec);
}

#nullable disable
