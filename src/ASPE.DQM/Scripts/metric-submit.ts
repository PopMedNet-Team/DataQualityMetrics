import Vue from 'vue';
import '@progress/kendo-ui';
import '@progress/kendo-theme-default/dist/all.css';
import axios from 'axios';
import { AutoComplete, ComboBox, DropDownList, MultiSelect, DropdownsInstaller } from '@progress/kendo-dropdowns-vue-wrapper';
import { Validator, ValidatorInstaller } from '@progress/kendo-validator-vue-wrapper';
import { debug, log } from 'util';
import $ from 'jquery';
import { IMetric, Metric, IMetricDependenciesResponse, IDomainItem, IResultsTypesItem, IDataQualtityFrameworkCategoryItem, DataQualityFrameworkCategoryItem, MetricDependenciesProvider, MetricResultsTypes } from './data-common';
import _ from 'lodash';

Vue.use(DropdownsInstaller);
Vue.use(ValidatorInstaller);

interface ISimilarMetric {
    id: any;
    title: string;
    description: string | null;
    resultsType: IResultsTypesItem | null;
    domains: IDomainItem[] | null;
    frameworkCategories: DataQualityFrameworkCategoryItem[] | null;
}

axios.get<IMetricDependenciesResponse>('/api/metrics/dependencies')
    .then(values => {
        let metric = new Metric();
        let depends = new MetricDependenciesProvider(values.data);
        //don't need to have the dependencies track changes
        //Object.freeze(depends);

        let vue = new Vue({
            el: '#vue_metric_submit',
            data: function () {
                return {
                    metric: metric,
                    dependencies: depends,
                    similar: [] as ISimilarMetric[]
                };
            },
            components: {
                Validator
            },
            created: function () {
                this.debouncedGetSimilarMetrics = _.debounce(this.getSimilarMetrics, 700, {'maxWait': 2500 });
            },
            mounted: function () {
                this.metric.resultsTypeID = MetricResultsTypes.Count;
            },
            methods: {
                onSave: function (event) {
                    let validator = (<any>this).kendoValidator;
                    if (!validator.validate()) {
                        return false;
                    }

                    axios.post<Metric>('/api/metrics/new', this.metric)
                        .then((response) => {
                            window.location.href = '/metric/' + response.data.id + '/edit';
                            return true;
                        });
                },
                getSimilarMetrics: function () {
                    let self = this;
                    axios.post<any>('/api/metrics/find', { resultsTypeID: this.metric.resultsTypeID, domains: this.metric.domains, frameworkCategories: this.metric.frameworkCategories })
                        .then((response: any) => {
                            self.similar = _.map(response.data as ISimilarMetric[], (metric) => {
                                //convert the frameworkCategories to class that has a display title
                                metric.frameworkCategories = _.map((metric.frameworkCategories || []), (category) => new DataQualityFrameworkCategoryItem(category));
                                return metric;
                            });
                        });

                },
                debouncedGetSimilarMetrics: function () { }
            },
            watch: {
                'metric.resultsTypeID': function (newValue, oldValue) {
                    this.debouncedGetSimilarMetrics();
                },
                'metric.frameworkCategories': function (newValue, oldValue) {
                    this.debouncedGetSimilarMetrics();
                },
                'metric.domains': function (newValue, oldValue) {
                    this.debouncedGetSimilarMetrics();
                }
            }
        });


    });