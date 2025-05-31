using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Interactivity;
using Interactivity.Pagination;
using Newtonsoft.Json;
using ShrekBot.Modules.Data_Files_and_Management;

namespace ShrekBot.Modules.Swamp.Modules
{
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _commands;
        //private readonly IServiceProvider _map;
        //private readonly InteractivityService _interactivity; //for pagination only

        public HelpModule(/*IServiceProvider map,*/ CommandService commands/*, InteractivityService interactiveService*/)
        {
            _commands = commands;
            //_map = map;
            //_interactivity = interactiveService;
        }

        [Command("helpowner", RunMode = RunMode.Async)]
        [Alias("owner", "help2")]
        [Summary("Lists the owner only commands and how to use them. For me only.")]
        [Remarks("Donkey! You better make sure only you know this!")]
        [RequireOwner]
        public async Task OwnerHelp()
        {
            EmbedBuilder builder = SetUpEmbedBuilder("Shrek's Hidden Onion Vault");
            List<CommandInfo> ownerOnlyCommands = new List<CommandInfo>();
            List<CommandInfo> userOnlyCommands = new List<CommandInfo>();

            SearchAllCommands(_commands.Modules.Where(m => m.Parent == null), 
                ref ownerOnlyCommands, ref userOnlyCommands); 

            foreach (CommandInfo item in ownerOnlyCommands)
                AddEmbedBuilderFields(item, ref builder);

            await ReplyAsync("", false, builder.Build());
        }

        [Command("help", RunMode = RunMode.Async)]
        [Summary("Lists all general user commands and how to use them.")]
        [Remarks("I only help Donkey. And you, I guess.")]
        public async Task Help()
        {
            EmbedBuilder builder = SetUpEmbedBuilder("Shrek's Onion Vault");            
            List<CommandInfo> ownerOnlyCommands = new List<CommandInfo>();
            List<CommandInfo> userCommands = new List<CommandInfo>();

            SearchAllCommands(_commands.Modules.Where(m => m.Parent == null),
                ref ownerOnlyCommands, ref userCommands);

            foreach (CommandInfo item in userCommands)
                AddEmbedBuilderFields(item, ref builder);

            await ReplyAsync("", false, builder.Build());
        }


        //if (precon.TypeId.ToString() == "Discord.Commands.RequireOwnerAttribute")
        //command.CheckPreconditionsAsync(Context, _map).GetAwaiter().GetResult();
        private bool CheckIfPreconHasOwner(IReadOnlyList<PreconditionAttribute> preconditions)
            => preconditions.Any(p => p is RequireOwnerAttribute);

        private EmbedBuilder SetUpEmbedBuilder(string title)
        {
            return new EmbedBuilder()
            {
                Title = title,
                Color = Color.DarkBlue,
                Footer = new EmbedFooterBuilder()
                .WithText("Note: <A value MUST in be here> | [A value in here is optional]")
            };
        }

        public void AddCommands(IReadOnlyList<CommandInfo> commands, 
            ref List<CommandInfo> ownerCommands, ref List<CommandInfo> userCommands)
        {
            foreach (CommandInfo command in commands)
            {
                if (CheckIfPreconHasOwner(command.Preconditions))
                    ownerCommands.Add(command);
                else
                    userCommands.Add(command);
            }               
        }

        public void AddEmbedBuilderFields(CommandInfo command, ref EmbedBuilder builder)
        {
            builder.AddField(f =>
            {
                f.Name = $"**{command.Name}**";
                f.Value = $"{command.Summary}\n" +
                (!string.IsNullOrEmpty(command.Remarks) ? $"({command.Remarks})\n" : "") +
                (command.Aliases.Any() ? $"**Aliases:** {string.Join(", ", command.Aliases.Select(x => $"`{x}`"))}\n" : "") +
                $"**Usage:** {GetAliases(command)}";
            });
        }

        public string GetAliases(CommandInfo command)
        {
            StringBuilder output = new StringBuilder($"`{Configuration.Config.bot.Prefix}");
            if (command.Module.IsSubmodule)
                output.Append($"{command.Module.Group} {command.Name}`");         
            else
                output.Append($"{command.Name}`");
            
            if (!command.Parameters.Any())
                return output.ToString();
            // <> required, ...Name, [] optional
            foreach (ParameterInfo param in command.Parameters)
            {
                if (param.IsOptional)
                    output.Append($" __[{param.Name}]__");
                else if (param.IsMultiple)
                    output.Append($" |*{param.Name}*|").ToString().TrimEnd('|');
                else if(param.IsRemainder)
                    output.Append($" ...{param.Name}");
                else
                    output.Append($" *<{param.Name}>*");
            }
            return output.ToString();
        }

        private void SearchAllCommands(IEnumerable<ModuleInfo> modules,
            ref List<CommandInfo> ownerCommands, ref List<CommandInfo> userCommands)
        {
            foreach (ModuleInfo module in modules)
            {
                if (CheckIfPreconHasOwner(module.Preconditions))
                    ownerCommands.AddRange(module.Commands);
                else
                {
                    AddCommands(module.Commands, ref ownerCommands, ref userCommands);
                }
                //submodule search
                foreach (ModuleInfo subModules in module.Submodules)
                {
                    if (CheckIfPreconHasOwner(subModules.Preconditions))
                    {
                        ownerCommands.AddRange(subModules.Commands);
                    }
                    else
                    {
                        //add commands here
                        AddCommands(subModules.Commands, ref ownerCommands, ref userCommands);
                    }
                }
            }
        }
    }
}
