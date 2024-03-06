const { PurgeCSSPlugin } = require('purgecss-webpack-plugin');

const { BundleAnalyzerPlugin } = require('webpack-bundle-analyzer');

const glob = require('glob');
const path = require('path');

module.exports = {
  plugins: [
     new PurgeCSSPlugin({
       paths: glob.sync(`${path.join(__dirname, 'src')}/**/*`, { nodir: true }),
       safelist: {
        standard: [
          /^p-/,      // Safelists all classes starting with 'p-' (for PrimeNG)
          /.*ng-.*/,  // Safelists any class that starts with 'ng-'
        ],

      }
     }),
    new BundleAnalyzerPlugin({
      analyzerMode: 'static' // This opens an HTML file with the bundle report
    })
  ]
};
