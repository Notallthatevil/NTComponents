import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { generateNotes } from '@semantic-release/release-notes-generator';

test('release tooling renders fixes and breaking changes with the configured preset', async () => {
    const config = JSON.parse(readFileSync(new URL('../.releaserc.json', import.meta.url), 'utf8'));
    const [, pluginConfig] = config.plugins.find(([name]) => name === '@semantic-release/release-notes-generator');
    const notes = await generateNotes(pluginConfig, {
        cwd: process.cwd(),
        options: { repositoryUrl: config.repositoryUrl },
        commits: [
            { hash: 'a'.repeat(40), message: 'fix(loader): animate during prerendering' },
            { hash: 'b'.repeat(40), message: 'feat(grid)!: revise column configuration\n\nBREAKING CHANGE: Columns require explicit keys.' }
        ],
        lastRelease: { gitTag: 'v1.0.0', gitHead: 'c'.repeat(40) },
        nextRelease: { version: '2.0.0-preview.1', gitTag: 'v2.0.0-preview.1', gitHead: 'd'.repeat(40) },
        logger: { log() {} }
    });

    assert.ok(notes.includes('animate during prerendering'));
    assert.ok(notes.includes('revise column configuration'));
    assert.ok(notes.includes('Columns require explicit keys.'));
});
