import type { Interval } from 'luxon'

export const TIME_FORMAT = 'HH:mm'
export const LONG_DAY_FORMAT = 'cccc d LLLL'
export const SHORT_DAY_FORMAT = 'ccc d LLL'

export const formatPeriod = (period: Interval) => period.toFormat(TIME_FORMAT, { separator: '-' })
