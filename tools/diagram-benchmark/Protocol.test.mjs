// Copyright 2026 Starion Group S.A. SPDX-License-Identifier: Apache-2.0

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { messages, summarizeTraffic, distribution } from './Protocol.mjs';

test('binary envelopes separate heartbeats and probes from application and shared-control messages', () => {
    const frame = text => { const payload = Buffer.from(text); return Buffer.concat([Buffer.from([payload.length]), payload]); };
    assert.deepEqual(messages(Buffer.from([2, 0x91, 6])), [{ category: 'heartbeat', bytes: 3 }]);
    const combined = Buffer.concat([frame('WatchBenchmarkGeometry'), frame('OnRenderCompleted'), frame('JsOnClickOutside')]);
    assert.deepEqual(messages(combined).map(item => item.category), ['probe', 'application', 'shared-control']);
    assert.equal(messages(frame('diagram-benchmark'))[0].category, 'probe');
    assert.equal(messages(Buffer.from([0xff, 0xff]))[0].category, 'unclassified');
    assert.equal(messages('{"protocol":"blazorpack"}')[0].category, 'transport');
});

test('traffic summaries retain raw frames and distinguish continuous from completed traffic', () => {
    const result = summarizeTraffic([
        { time: 1, direction: 'sent', bytes: 10, messages: [{ category: 'application', bytes: 10 }] },
        { time: 3, direction: 'received', bytes: 8, messages: [{ category: 'probe', bytes: 5 }, { category: 'application', bytes: 3 }] }
    ], 0, 2, 4);
    assert.equal(result.raw.sentframes, 1);
    assert.equal(result.raw.receivedbytes, 8);
    assert.equal(result.application.duringsentbytes, 10);
    assert.equal(result.application.completedreceivedbytes, 3);
    assert.equal(result.probe.receivedbytes, 5);
    assert.equal(distribution(Array.from({ length: 30 }, (_, index) => index + 1)).p95, 29);
});
