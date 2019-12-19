"use strict";
const path = require('path');
const CleanWebpackPlugin = require('clean-webpack-plugin');
const HtmlWebpackDeployAssetsPlugin = require('html-webpack-deploy-assets-plugin');

module.exports = {
    entry: {
        page: './scripts/page.ts',
        registration: './scripts/registration.ts',
        registerVisualization: './scripts/RegisterVisualization.ts',
        'visualizations-list': './scripts/visualizations-list.ts',
        'metrics-list': './scripts/metrics-list.ts',
        'metric-details': './scripts/metric-details.ts',
        'metric-edit': './scripts/metric-edit.ts',
        'metric-submit': './scripts/metric-submit.ts',
        'measures-submit': './scripts/measures-submit.ts',
        'measures-manage': './scripts/measures-manage.ts',
        'dashboard': './scripts/dashboard.ts',
        'visual': './scripts/visual.ts',
        test: './scripts/test.ts'
    },
    plugins: [
        new CleanWebpackPlugin(['./wwwroot/scripts']),
        new HtmlWebpackDeployAssetsPlugin({
            "packages": {
                "bootstrap": {
                    "assets": {
                        "dist/css": "css/",
                        "dist/js": "js/"
                    },
                    "entries": []
                },
                "jquery": {
                    "assets": {
                        "dist": "./"
                    }
                }
            },
            "outputPath": "../assets/[name]"
        })
    ],
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                loader: 'ts-loader'
            },
            {
                test: /\.css$/,
                use: ['style-loader', 'css-loader']
            },
            {
                test: require.resolve('jquery'),
                use: [{loader: 'expose-loader', options: '$' }]
            },
            {
                test: require.resolve('lodash'),
                use: [{loader: 'expose-loader', options: '_' }]
            }
        ]
    },
    resolve: {
        alias: {
            'vue$': 'vue/dist/vue.esm.js'
        },
        extensions: ['.tsx', '.ts', '.js', '.vue', '.json']
    },
    output: {
        filename: '[name].js',
        path: path.resolve(__dirname, 'wwwroot/scripts')
    },
    optimization: {
        splitChunks: {
            cacheGroups: {
                commons: {
                    name: 'commons',
                    chunks: 'initial',
                    minChunks: 2
                }
            }
        }
    }
};