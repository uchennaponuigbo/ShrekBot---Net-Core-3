using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace ShrekBot.Modules.Swamp
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal sealed class Custom_ModuleAliasAttribute : PreconditionAttribute
    {
        private readonly string _name;

        // maybe an array of strings, incase I want different alias of the modules?
        public Custom_ModuleAliasAttribute(string name) => _name = name;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, 
            CommandInfo command, IServiceProvider services)
        {
            return Task.FromResult(PreconditionResult.FromSuccess());
        }

        /// <summary>
        /// Use this to get the alias of the module
        /// </summary>
        /// <returns>The alias I used to label this module</returns>
        public override string ToString() => _name;
        
    }
}
