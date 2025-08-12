#nullable enable

using System.Threading.Tasks;
using PluginRight.Core.Models;

namespace PluginRight.Core.Interfaces;

public interface IModelClient
{
    Task<string> GenerateLogicAsync(Spec spec);
}

#nullable disable
