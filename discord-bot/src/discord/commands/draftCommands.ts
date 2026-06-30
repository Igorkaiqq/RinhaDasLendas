import { SlashCommandBuilder } from 'discord.js'
import { DraftCommandNames, DraftOptionNames, DraftPickOrderMode } from '../../shared/constants/draftConstants/index.js'
import { t } from '../../shared/messages/index.js'

export const draftCommands = [
  new SlashCommandBuilder()
    .setName(DraftCommandNames.Create)
    .setDescription(t.commands.createDescription)
    .addStringOption((option) => option.setName(DraftOptionNames.Name).setDescription(t.commands.nameOption).setRequired(true))
    .addStringOption((option) => option.setName(DraftOptionNames.Day).setDescription(t.commands.dayOption).setRequired(true))
    .addStringOption((option) => option.setName(DraftOptionNames.Time).setDescription(t.commands.timeOption).setRequired(true))
    .addStringOption((option) => option.setName(DraftOptionNames.Note).setDescription(t.commands.noteOption).setRequired(false)),
  new SlashCommandBuilder().setName(DraftCommandNames.Status).setDescription(t.commands.statusDescription),
  new SlashCommandBuilder().setName(DraftCommandNames.List).setDescription(t.commands.listDescription),
  new SlashCommandBuilder()
    .setName(DraftCommandNames.Cancel)
    .setDescription(t.commands.cancelDescription)
    .addStringOption((option) => option.setName(DraftOptionNames.DraftId).setDescription(t.commands.draftIdOption).setRequired(true))
    .addStringOption((option) => option.setName(DraftOptionNames.Reason).setDescription(t.commands.reasonOption).setRequired(false)),
  new SlashCommandBuilder()
    .setName(DraftCommandNames.ClosePresence)
    .setDescription(t.commands.closePresenceDescription)
    .addStringOption((option) => option.setName(DraftOptionNames.DraftId).setDescription(t.commands.draftIdOption).setRequired(true)),
  new SlashCommandBuilder()
    .setName(DraftCommandNames.DefineCaptains)
    .setDescription(t.commands.defineCaptainsDescription)
    .addStringOption((option) => option.setName(DraftOptionNames.DraftId).setDescription(t.commands.draftIdOption).setRequired(true))
    .addStringOption((option) => option.setName(DraftOptionNames.CaptainIds).setDescription(t.commands.captainIdsOption).setRequired(true)),
  new SlashCommandBuilder()
    .setName(DraftCommandNames.DefinePickOrder)
    .setDescription(t.commands.definePickOrderDescription)
    .addStringOption((option) => option.setName(DraftOptionNames.DraftId).setDescription(t.commands.draftIdOption).setRequired(true))
    .addStringOption((option) =>
      option
        .setName(DraftOptionNames.Mode)
        .setDescription(t.commands.modeOption)
        .setRequired(true)
        .addChoices(
          { name: t.commands.modeDrawn, value: DraftPickOrderMode.Drawn },
          { name: t.commands.modeManual, value: DraftPickOrderMode.Manual },
        ),
    )
    .addStringOption((option) => option.setName(DraftOptionNames.CaptainIds).setDescription(t.commands.manualCaptainIdsOption).setRequired(false)),
].map((command) => command.toJSON())
