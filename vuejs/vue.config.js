const CopyWebpackPlugin = require('copy-webpack-plugin');

module.exports = {
    outputDir: '../wwwroot',
    configureWebpack: {
      plugins: [
        new CopyWebpackPlugin([
            { from: 'node_modules/oidc-client/dist/oidc-client.min.js', to: 'js' }
        ])
      ]
    }
  }