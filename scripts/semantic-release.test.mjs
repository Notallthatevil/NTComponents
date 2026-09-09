import test from 'node:test';
import assert from 'node:assert/strict';
import { execFile, execFileSync } from 'node:child_process';
import { copyFileSync, mkdtempSync, mkdirSync, readFileSync, rmSync } from 'node:fs';
import { createServer } from 'node:http';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { promisify } from 'node:util';

const run = promisify(execFile);
const root = fileURLToPath(new URL('../', import.meta.url));
const cli = join(root, 'node_modules/semantic-release/bin/semantic-release.js');
const { repositoryUrl } = JSON.parse(readFileSync(join(root, '.releaserc.json'), 'utf8'));
const repository = new URL(repositoryUrl).pathname.slice(1).replace(/\.git$/, '');

for (const [branch, version] of [['main', '2.0.0'], ['preview', '2.0.0-preview.1']]) {
    test(`semantic-release CLI dry run validates the ${branch} pipeline`, { timeout: 60000 }, async t => {
        const directory = mkdtempSync(join(tmpdir(), 'ntcomponents-release-'));
        const work = join(directory, 'work');
        const remote = join(directory, 'remote.git');
        mkdirSync(work);
        // Both paths are beneath this test's uniquely created temporary directory.
        t.after(() => rmSync(directory, { recursive: true, force: true, maxRetries: 3 }));

        // Do not inherit CI credentials, Git hooks/configuration, or proxy settings.
        const env = {
            PATH: process.env.PATH,
            SYSTEMROOT: process.env.SYSTEMROOT,
            WINDIR: process.env.WINDIR,
            COMSPEC: process.env.COMSPEC,
            TEMP: directory,
            TMP: directory,
            HOME: directory,
            USERPROFILE: directory,
            GIT_CONFIG_NOSYSTEM: '1',
            GIT_CONFIG_GLOBAL: join(directory, 'gitconfig'),
            GIT_TERMINAL_PROMPT: '0',
            GIT_ALLOW_PROTOCOL: 'file',
            GIT_AUTHOR_NAME: 'Release test',
            GIT_AUTHOR_EMAIL: 'release-test@example.invalid',
            GIT_COMMITTER_NAME: 'Release test',
            GIT_COMMITTER_EMAIL: 'release-test@example.invalid',
            CI: 'true',
            GITHUB_ACTIONS: 'true',
            GITHUB_ACTION: 'release-test',
            GITHUB_EVENT_NAME: 'push',
            GITHUB_REF: `refs/heads/${branch}`,
            GITHUB_REF_NAME: branch,
            GITHUB_REPOSITORY: repository,
            GITHUB_TOKEN: 'local-test-token',
            NO_COLOR: '1'
        };
        const git = (...args) => execFileSync('git', args, { cwd: work, env, encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }).trim();
        git('config', '--global', `url.${pathToFileURL(remote).href}.insteadOf`, repositoryUrl);
        git('init', '--bare', '--initial-branch=main', remote);
        git('init', '--initial-branch=main');
        copyFileSync(join(root, '.releaserc.json'), join(work, '.releaserc.json'));
        git('add', '.releaserc.json');
        git('commit', '-m', 'chore: initialize release fixture');
        git('tag', 'v1.0.0');
        git('branch', 'preview');
        git('checkout', branch);
        git('commit', '--allow-empty', '-m', 'fix(loader): animate during prerendering');
        git('commit', '--allow-empty', '-m', 'feat(grid)!: revise column configuration', '-m', 'BREAKING CHANGE: Columns require explicit keys.');
        git('remote', 'add', 'origin', repositoryUrl);
        git('push', 'origin', '--all');
        git('push', 'origin', '--tags');
        const refsBefore = git('ls-remote', 'origin');
        const tagsBefore = git('tag', '--list');

        const requests = [];
        const api = createServer((request, response) => {
            requests.push(`${request.method} ${request.url}`);
            // Verification is the only allowed request. Publishing must never run.
            if (request.method !== 'GET' || request.url !== `/repos/${repository}`) {
                response.writeHead(400).end('Unexpected release API request');
                return;
            }
            response.writeHead(200, { 'content-type': 'application/json' });
            response.end(JSON.stringify({ clone_url: repositoryUrl, permissions: { push: true } }));
        });
        await new Promise((resolve, reject) => {
            api.once('error', reject);
            api.listen(0, '127.0.0.1', resolve);
        });
        t.after(() => new Promise(resolve => api.close(resolve)));
        env.GITHUB_API_URL = `http://127.0.0.1:${api.address().port}`;

        let output;
        try {
            const result = await run(process.execPath, [cli, '--dry-run'], { cwd: work, env, timeout: 45000, maxBuffer: 1024 * 1024 });
            output = result.stdout + result.stderr;
        } catch (error) {
            assert.fail(`semantic-release dry run failed:\n${error.stdout ?? ''}\n${error.stderr ?? error.message}`);
        }

        assert.ok(output.includes(`The next release version is ${version}`), output);
        assert.ok(output.includes('animate during prerendering'), output);
        assert.ok(output.includes('Columns require explicit keys.'), output);
        assert.ok(output.includes('Skip step "publish"'), output);
        assert.deepEqual(requests, [`GET /repos/${repository}`]);
        assert.equal(git('ls-remote', 'origin'), refsBefore, 'Dry run changed remote refs');
        assert.equal(git('tag', '--list'), tagsBefore, 'Dry run created a release tag');
    });
}
