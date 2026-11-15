import { build } from 'esbuild'
import { readFileSync, writeFileSync, copyFileSync, mkdirSync, readdirSync } from 'fs'

const name = 'WebSharper.GoldenLayout.Sample'

// The WebSharper-generated module imports the npm `golden-layout` package and
// the WebSharper runtime; bundle it into a single IIFE for the static page.
await build({
  entryPoints: [`./build/${name}.js`],
  bundle: true,
  minify: true,
  format: 'iife',
  outfile: `../dist/Scripts/${name}.min.js`,
  globalName: 'wsbundle'
})

// Golden Layout ships its own stylesheets: the base layout CSS plus a set of
// themes, which reference the header icons through `../../img/`. The page
// links them directly and swaps the theme with the theme toggle.
mkdirSync('../dist/css/themes', { recursive: true })
mkdirSync('../dist/img', { recursive: true })
copyFileSync('node_modules/golden-layout/dist/css/goldenlayout-base.css', '../dist/css/goldenlayout-base.css')
copyFileSync('node_modules/golden-layout/dist/css/themes/goldenlayout-light-theme.css', '../dist/css/themes/goldenlayout-light-theme.css')
copyFileSync('node_modules/golden-layout/dist/css/themes/goldenlayout-dark-theme.css', '../dist/css/themes/goldenlayout-dark-theme.css')
for (const img of readdirSync('node_modules/golden-layout/dist/img')) {
  copyFileSync(`node_modules/golden-layout/dist/img/${img}`, `../dist/img/${img}`)
}

copyFileSync('./site.css', '../dist/site.css')

// Fold the WebSharper head (linked resources) into the page.
const head = readFileSync(`./build/${name}.head.html`, 'utf8')
const html = readFileSync('./index.html', 'utf8').replace('<!-- ws-head -->', head)
writeFileSync('../dist/index.html', html)

console.log('Bundled:', name)