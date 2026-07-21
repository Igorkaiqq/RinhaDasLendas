import assert from 'node:assert/strict'
import { describe, it } from 'node:test'
import { PermissionFlagsBits } from 'discord.js'

import { draftCommands } from './draftCommands.js'

describe('draftCommands', () => {
  it('requires ManageGuild only for mutable commands', () => {
    const mutable = new Set([
      'draft-criar',
      'draft-encerrar-presenca',
      'draft-definir-capitaes',
      'draft-definir-ordem-escolha',
    ])
    const expectedCommands = new Set([...mutable, 'draft-listar', 'draft-status'])

    assert.deepEqual(new Set(draftCommands.map((command) => command.name)), expectedCommands)

    for (const command of draftCommands) {
      assert.equal(
        command.default_member_permissions,
        mutable.has(command.name) ? PermissionFlagsBits.ManageGuild.toString() : undefined,
      )
    }
  })
})
