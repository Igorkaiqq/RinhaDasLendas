// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { defineComponent } from 'vue'

import { Permissions } from '@/constants/permissions'
import { AuthRoles } from '@/constants/authRoles'
import { i18n } from '@/i18n'
import { setPermissions } from '@/services/authState'

import SettingsView from './SettingsView.vue'

vi.mock('vue-router', () => ({ useRoute: () => ({ query: {} }) }))

const Stub = (name: string, marker: string) => defineComponent({ name, template: `<section ${marker}></section>` })

function mountView() {
  return mount(SettingsView, {
    global: {
      plugins: [i18n],
      stubs: {
        DiscordLinkSection: Stub('DiscordLinkSection', 'data-discord-link'),
        DiscordAdminConfigurationSection: Stub('DiscordAdminConfigurationSection', 'data-sensitive-config'),
        PresenceScheduleSection: Stub('PresenceScheduleSection', 'data-presence-schedules'),
      },
    },
  })
}

describe('SettingsView permissions', () => {
  afterEach(() => setPermissions(null))

  it('hides management from a regular player', () => {
    setPermissions({ permissions: [], roles: [AuthRoles.Jogador], effectiveRole: AuthRoles.Jogador })
    const wrapper = mountView()
    expect(wrapper.find('[data-presence-schedules]').exists()).toBe(false)
    expect(wrapper.find('[data-sensitive-config]').exists()).toBe(false)
  })

  it('shows schedules but not sensitive configuration to a moderator', () => {
    setPermissions({ permissions: [Permissions.CanManageDrafts], roles: [AuthRoles.Moderador], effectiveRole: AuthRoles.Moderador })
    const wrapper = mountView()
    expect(wrapper.find('[data-presence-schedules]').exists()).toBe(true)
    expect(wrapper.find('[data-sensitive-config]').exists()).toBe(false)
  })

  it('keeps sensitive configuration separate for an administrator', () => {
    setPermissions({ permissions: [Permissions.CanManageDrafts, Permissions.CanManageUsers], roles: [AuthRoles.Admin], effectiveRole: AuthRoles.Admin })
    const wrapper = mountView()
    expect(wrapper.find('[data-presence-schedules]').exists()).toBe(true)
    expect(wrapper.find('[data-sensitive-config]').exists()).toBe(true)
  })
})
