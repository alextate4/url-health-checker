// Run with: npm --prefix web test   (Node's built-in runner; no packages)
import { test } from 'node:test'
import assert from 'node:assert/strict'
import { nextSort, sortArrow, pageCount, rowStatus, formatPercent, formatMs, checkTooltip, checkSecondsOrDefault, retentionDaysOrDefault } from './logic.js'

test('clicking the sorted column flips its direction, both ways', () => {
  assert.deepEqual(nextSort({ key: 'url', dir: 'asc' }, 'url'), { key: 'url', dir: 'desc' })
  assert.deepEqual(nextSort({ key: 'url', dir: 'desc' }, 'url'), { key: 'url', dir: 'asc' })
})

test('clicking a different column sorts it ascending', () => {
  assert.deepEqual(nextSort({ key: 'url', dir: 'desc' }, 'uptime'), { key: 'uptime', dir: 'asc' })
})

test('every sortable header shows an arrow; only the sorted one shows its direction', () => {
  const sort = { key: 'url', dir: 'desc' }
  assert.equal(sortArrow(sort, 'url'), '▼')
  assert.equal(sortArrow({ key: 'url', dir: 'asc' }, 'url'), '▲')
  assert.equal(sortArrow(sort, 'code'), '↕')
})

test('page count rounds up and is never zero', () => {
  assert.equal(pageCount(25, 10), 3)
  assert.equal(pageCount(100, 50), 2)
  assert.equal(pageCount(0, 10), 1)
})

test('row status: pending until the first check, then the last result', () => {
  assert.equal(rowStatus({ lastCheck: null }).word, 'Pending')
  assert.equal(rowStatus({ lastCheck: { healthy: true } }).className, 'healthy')
  assert.equal(rowStatus({ lastCheck: { healthy: false } }).word, 'Unhealthy')
})

test('a real 0 shows as 0, and only a missing value shows a dash', () => {
  assert.equal(formatPercent(0), '0%')
  assert.equal(formatPercent(null), '—')
  assert.equal(formatMs(0), '0 ms')
  assert.equal(formatMs(null), '—')
})

test('a check with no response says so in its tooltip', () => {
  const text = checkTooltip({ healthy: false, statusCode: null, responseTimeMs: 12, checkedAt: '2026-11-07T18:00:00Z' })
  assert.match(text, /^Unhealthy · no response · 12 ms · /)
})

test('check interval: 60 s when missing or broken, never below 10 s', () => {
  assert.equal(checkSecondsOrDefault(undefined), 60) // an old server sends no field
  assert.equal(checkSecondsOrDefault(null), 60)
  assert.equal(checkSecondsOrDefault('abc'), 60)
  assert.equal(checkSecondsOrDefault(0), 60)
  assert.equal(checkSecondsOrDefault(3), 10)
  assert.equal(checkSecondsOrDefault('45'), 45)
})

test('retention: 30 days when missing or 0, never below 1 day', () => {
  assert.equal(retentionDaysOrDefault(undefined), 30)
  assert.equal(retentionDaysOrDefault(0), 30)
  assert.equal(retentionDaysOrDefault(7), 7)
})
