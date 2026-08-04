import assert from "node:assert/strict";
import {
    mkdirSync,
    mkdtempSync,
    rmSync,
    writeFileSync
} from "node:fs";
import { dirname, join } from "node:path";
import { tmpdir } from "node:os";
import { afterEach, test } from "node:test";

import {
    convertLcovToSonarXml,
    escapeXmlAttribute
} from "./ConvertLcovToSonar.mjs";

const sandboxes = new Set();

afterEach(() => {
    for (const sandbox of sandboxes) {
        rmSync(sandbox, { force: true, recursive: true });
    }

    sandboxes.clear();
});

test("converter emits covered and uncovered executable lines", () => {
    const repositoryRoot = createRepository(["src/module.js"]);
    const xml = convert([
        "SF:src/module.js",
        "DA:1,3",
        "DA:2,0",
        "end_of_record"
    ], repositoryRoot);

    assert.equal(xml, [
        '<?xml version="1.0" encoding="UTF-8"?>',
        '<coverage version="1">',
        '  <file path="src/module.js">',
        '    <lineToCover lineNumber="1" covered="true" />',
        '    <lineToCover lineNumber="2" covered="false" />',
        "  </file>",
        "</coverage>",
        ""
    ].join("\n"));
});

test("converter aggregates branch totals and covered branches per line", () => {
    const repositoryRoot = createRepository(["src/branches.js"]);
    const xml = convert([
        "SF:src/branches.js",
        "DA:4,2",
        "BRDA:4,0,0,1",
        "BRDA:4,0,1,0",
        "BRDA:4,0,2,-",
        "end_of_record"
    ], repositoryRoot);

    assert.match(
        xml,
        /lineNumber="4" covered="true" branchesToCover="3" coveredBranches="1"/
    );
});

test("converter sorts files and executable lines deterministically", () => {
    const repositoryRoot = createRepository(["src/alpha.js", "src/zeta.js"]);
    const lcovLines = [
        "SF:src/zeta.js",
        "DA:10,1",
        "DA:2,1",
        "end_of_record",
        "SF:src/alpha.js",
        "DA:3,1",
        "end_of_record"
    ];
    const firstXml = convert(lcovLines, repositoryRoot);
    const secondXml = convert(lcovLines, repositoryRoot);

    assert.equal(firstXml, secondXml);
    assert.ok(firstXml.indexOf('path="src/alpha.js"') < firstXml.indexOf('path="src/zeta.js"'));
    assert.ok(firstXml.indexOf('lineNumber="2"') < firstXml.indexOf('lineNumber="10"'));
});

test("converter normalizes Windows source separators", () => {
    const repositoryRoot = createRepository(["src/nested/module.js"]);
    const xml = convert([
        "SF:src\\nested\\module.js",
        "DA:1,1",
        "end_of_record"
    ], repositoryRoot);

    assert.match(xml, /<file path="src\/nested\/module\.js">/);
    assert.doesNotMatch(xml, /\\/);
});

test("converter XML-escapes source paths", () => {
    const repositoryRoot = createRepository(["src/Ampersand & Module.js"]);
    const xml = convert([
        "SF:src/Ampersand & Module.js",
        "DA:1,1",
        "end_of_record"
    ], repositoryRoot);

    assert.match(xml, /path="src\/Ampersand &amp; Module\.js"/);
    assert.equal(escapeXmlAttribute(`"'&<>`), "&quot;&apos;&amp;&lt;&gt;");
});

test("converter rejects malformed numeric fields", () => {
    const repositoryRoot = createRepository(["src/module.js"]);

    assert.throws(
        () => convert([
            "SF:src/module.js",
            "DA:not-a-line,1",
            "end_of_record"
        ], repositoryRoot),
        /malformed DA line number/
    );
});

test("converter rejects absolute source paths", () => {
    const repositoryRoot = createRepository([]);

    assert.throws(
        () => convert([
            "SF:/outside/module.js",
            "DA:1,1",
            "end_of_record"
        ], repositoryRoot),
        /absolute SF path is not allowed/
    );
});

test("converter rejects missing source files", () => {
    const repositoryRoot = createRepository([]);

    assert.throws(
        () => convert([
            "SF:src/missing.js",
            "DA:1,1",
            "end_of_record"
        ], repositoryRoot),
        /SF source file does not exist/
    );
});

test("converter rejects source paths outside the repository", () => {
    const { repositoryRoot, sandbox } = createNestedRepository();
    writeFileSync(join(sandbox, "outside.js"), "// outside\n", "utf8");

    assert.throws(
        () => convert([
            "SF:../outside.js",
            "DA:1,1",
            "end_of_record"
        ], repositoryRoot),
        /SF path is outside the repository/
    );
});

test("converter rejects duplicate normalized source records", () => {
    const repositoryRoot = createRepository(["src/module.js"]);

    assert.throws(
        () => convert([
            "SF:src/module.js",
            "DA:1,1",
            "end_of_record",
            "SF:src\\module.js",
            "DA:1,1",
            "end_of_record"
        ], repositoryRoot),
        /duplicate source record/
    );
});

function convert(lines, repositoryRoot) {
    return convertLcovToSonarXml(`${lines.join("\n")}\n`, { repositoryRoot });
}

function createRepository(sourcePaths) {
    const { repositoryRoot } = createNestedRepository();

    for (const sourcePath of sourcePaths) {
        const absolutePath = join(repositoryRoot, ...sourcePath.split("/"));
        mkdirSync(dirname(absolutePath), { recursive: true });
        writeFileSync(absolutePath, "// source\n", "utf8");
    }

    return repositoryRoot;
}

function createNestedRepository() {
    const sandbox = mkdtempSync(join(tmpdir(), "bloom-lcov-"));
    const repositoryRoot = join(sandbox, "repository");
    sandboxes.add(sandbox);
    mkdirSync(repositoryRoot);
    return { repositoryRoot, sandbox };
}
