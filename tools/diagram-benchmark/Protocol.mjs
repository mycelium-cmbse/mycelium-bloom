// Copyright 2026 Starion Group S.A. SPDX-License-Identifier: Apache-2.0

export function messages(data) {
    if (typeof data === 'string') return [{ category: 'transport', bytes: Buffer.byteLength(data) }];
    const buffer = Buffer.from(data), result = [];
    let offset = 0;
    while (offset < buffer.length) {
        const start = offset;
        let length = 0, shift = 0, value;
        do {
            if (offset >= buffer.length || shift > 28) return [{ category: 'unclassified', bytes: buffer.length }];
            value = buffer[offset++];
            length += (value & 0x7f) * 2 ** shift;
            shift += 7;
        } while (value & 0x80);
        if (offset + length > buffer.length) return [{ category: 'unclassified', bytes: buffer.length }];
        const payload = buffer.subarray(offset, offset + length), text = payload.toString('utf8');
        let category = 'application';
        if (payload.length === 2 && payload[0] === 0x91 && payload[1] === 6) category = 'heartbeat';
        else if (text.includes('JsOnClickOutside')) category = 'shared-control';
        else if (/diagram-benchmark|WatchBenchmark|ReadBenchmark|BenchmarkPing/.test(text)) category = 'probe';
        result.push({ category, bytes: offset + length - start });
        offset += length;
    }
    return result;
}

export function summarizeTraffic(packets, start, release, end) {
    const result = { raw: {}, application: {}, probe: {}, heartbeat: {}, 'shared-control': {}, unclassified: {}, transport: {} };
    for (const packet of packets.filter(item => item.time >= start && item.time <= end)) {
        const direction = packet.direction, phase = packet.time <= release ? 'during' : 'completed';
        for (const key of ['frames', 'bytes']) result.raw[`${direction}${key}`] ??= 0;
        result.raw[`${direction}frames`]++;
        result.raw[`${direction}bytes`] += packet.bytes;
        const seen = new Set();
        for (const message of packet.messages) {
            const category = result[message.category];
            category[`${direction}messages`] = (category[`${direction}messages`] ?? 0) + 1;
            category[`${direction}bytes`] = (category[`${direction}bytes`] ?? 0) + message.bytes;
            category[`${phase}${direction}bytes`] = (category[`${phase}${direction}bytes`] ?? 0) + message.bytes;
            seen.add(message.category);
        }
        for (const name of seen) result[name][`${direction}frames`] = (result[name][`${direction}frames`] ?? 0) + 1;
    }
    return result;
}

export function percentile(values, quantile) {
    if (!values.length) return null;
    const sorted = [...values].sort((a, b) => a - b);
    return sorted[Math.max(0, Math.ceil(sorted.length * quantile) - 1)];
}

export function distribution(values) {
    return { count: values.length, p50: percentile(values, 0.5), p95: percentile(values, 0.95),
        min: values.length ? Math.min(...values) : null, max: values.length ? Math.max(...values) : null };
}
