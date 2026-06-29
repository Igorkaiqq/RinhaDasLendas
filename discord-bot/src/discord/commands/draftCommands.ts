import { SlashCommandBuilder } from 'discord.js'

export const draftCommands = [
  new SlashCommandBuilder()
    .setName('draft-criar')
    .setDescription('Cria um draft com lista de presença')
    .addStringOption((option) => option.setName('nome').setDescription('Nome do draft').setRequired(true))
    .addStringOption((option) => option.setName('dia').setDescription('Dia do encerramento. Ex: 29/06 ou 29/06/2026').setRequired(true))
    .addStringOption((option) => option.setName('horario').setDescription('Horário de Brasília. Ex: 21:30').setRequired(true))
    .addStringOption((option) => option.setName('observacao').setDescription('Observação opcional').setRequired(false)),
  new SlashCommandBuilder().setName('draft-status').setDescription('Mostra o status dos drafts ativos'),
  new SlashCommandBuilder().setName('draft-encerrar-presenca').setDescription('Encerra a presença de um draft').addStringOption((option) => option.setName('draft_id').setDescription('ID do draft').setRequired(true)),
  new SlashCommandBuilder()
    .setName('draft-definir-capitaes')
    .setDescription('Define capitães por IDs de jogadores separados por vírgula')
    .addStringOption((option) => option.setName('draft_id').setDescription('ID do draft').setRequired(true))
    .addStringOption((option) => option.setName('capitaes_ids').setDescription('IDs dos jogadores capitães').setRequired(true)),
  new SlashCommandBuilder()
    .setName('draft-definir-ordem-escolha')
    .setDescription('Define ordem de escolha')
    .addStringOption((option) => option.setName('draft_id').setDescription('ID do draft').setRequired(true))
    .addStringOption((option) => option.setName('modo').setDescription('Manual ou Sorteado').setRequired(true).addChoices({ name: 'Sorteado', value: 'Sorteado' }, { name: 'Manual', value: 'Manual' }))
    .addStringOption((option) => option.setName('capitaes_ids').setDescription('IDs dos capitães em ordem manual').setRequired(false)),
].map((command) => command.toJSON())
