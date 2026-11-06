import { Settings } from 'luxon'

declare module 'luxon' {
  interface TSSettings {
    throwOnInvalid: true
  }
}

// The whole app works in UTC. This also makes API times without a zone designator parse as UTC,
// which is how the API returns values read back from the database.
Settings.defaultZone = 'utc'
Settings.defaultLocale = 'en-GB'
Settings.throwOnInvalid = true
