import Vue from 'vue';
import axios, { AxiosResponse } from 'axios';
import { DropdownsInstaller } from '@progress/kendo-dropdowns-vue-wrapper';
import { formatDate } from '@telerik/kendo-intl';
import { orderBy, SortDescriptor } from '@progress/kendo-data-query';
import { Grid, GridNoRecords } from '@progress/kendo-vue-grid';
import { ApiResult, IMetricListItem, MetricStatuses, Guid, IMeasureMetadata, MeasureMetadata } from './data-common';
import Component from 'vue-class-component';
import $ from 'jquery';

Vue.use(DropdownsInstaller);

const statuses: { text: string, value: any }[] = [
    { text: 'All Statuses', value: Guid.empty },
    { text: 'Draft', value: MetricStatuses.Draft },
    { text: 'Submitted', value: MetricStatuses.Submitted },
    { text: 'In Review', value: MetricStatuses.InReview },
    { text: 'Published', value: MetricStatuses.Published },
    { text: 'Published* - requires authentication', value: MetricStatuses.PublishedRequiresAuthentication },
    { text: 'Rejected', value: MetricStatuses.Rejected },
    { text: 'Inactive', value: MetricStatuses.Inactive },
    { text: 'Deleted', value: MetricStatuses.Deleted }
];

Object.freeze(statuses);

const metricsVue = new Vue({
    el: '#vue_metrics',
    data: function () {
        return {
            metrics: <IMetricListItem[]>[],
            statuses: statuses,
            selectedStatusID: Guid.empty,
            loading: true
        };
    },
    filters: {
        localDateString: function (value): string {
            if (value) {
                return kendo.parseDate((value || '').toString() + 'Z').toLocaleDateString();
            }

            return "";
        },
        statusText: function (value): string {
            switch ((value || "").toLowerCase()) {
                case MetricStatuses.Draft:
                    return "Draft";
                case MetricStatuses.Submitted:
                    return "Submitted";
                case MetricStatuses.InReview:
                    return "In Review";
                case MetricStatuses.Published:
                    return "Published";
                case MetricStatuses.PublishedRequiresAuthentication:
                    return "Published*";
                case MetricStatuses.Rejected:
                    return "Rejected";
                case MetricStatuses.Inactive:
                    return "Inactive";
                case MetricStatuses.Deleted:
                    return "Deleted";
            }

            return "";
        }
    },
    computed: {
        filteredMetrics(): IMetricListItem[] {
            if (Guid.equals(Guid.empty, this.selectedStatusID)) {
                return this.metrics;
            }

            let selectedStatusID: any = this.selectedStatusID;
            return this.metrics.filter((item) => Guid.equals(item.status.metricStatusID, selectedStatusID));
        }
    },
    mounted: function () {
        let self = this;
        axios.get<IMetricListItem[]>('/api/metrics/list?returnMyOnly=true')
            .then(function (response: AxiosResponse<IMetricListItem[]>) {
                self.metrics = response.data;
                self.loading = false;
            });
    }
});

const componentInstance = Vue.component("template-component", {
    props: {
        dataItem: Object
    },
    template: `<div class="row measuremeta-details">
            <div class="col-12 col-lg-8">
                <label>Metric:</label> {{dataItem.metricTitle}}
            </div>
            <div class="col-12 col-lg-4 evenItem">
                <label>Results Type:</label> {{dataItem.resultsType}}
            </div>
            <div class="col-12 col-lg-4">
                <label>Submitted On:</label> <span class="nowrap">{{dataItem.submittedOnLocal | formatDate}}</span>
            </div>
            <div class="col-12 col-lg-4 evenItem">
                <label>Run Date:</label> <span class="nowrap">{{dataItem.runDateLocal | formatDate}}</span>
            </div>
            <div class="col-12 col-lg-4">
                <label>Date Range Start:</label> <span class="nowrap">{{dataItem.dateRangeStartLocal | formatDate}}</span>
            </div>
            <div class="col-12 col-lg-4 evenItem">
                <label>Date Range End:</label> <span class="nowrap">{{dataItem.dateRangeEndLocal | formatDate}}</span>
            </div>
            <div class="col-12 col-lg-4">
                <label>Suspended On:</label> <span class="nowrap">{{dataItem.suspendedOnLocal | formatDate}}</span>
            </div>
            <div class="col-12 col-lg-4 evenItem">
                <label>Organization:</label> {{dataItem.organization}}
            </div>
            <div class="col-12 col-lg-4">
                <label>Data Source:</label> {{dataItem.dataSource}}
            </div>
            <div class="col-12 col-lg-4 evenItem">
                <label>Network:</label> {{dataItem.network}}
            </div>
            <div class="col-12 col-lg-4">
                <label>Common Data Model:</label> {{dataItem.commonDataModel}}
            </div>
            <div class="col-12 col-lg-4 evenItem">
                <label>Common Data Model Version:</label> {{dataItem.commonDataModelVersion}}
            </div>
            <div class="col-12 col-lg-4">
                <label>Results Delimiter:</label> {{dataItem.resultsDelimiter}}
            </div>
            <div class="col-12 col-lg-4 evenItem">
                <label>Database System:</label> {{dataItem.databaseSystem}}
            </div>
            <div class="col-12 col-lg-8">
                <label>Supporting Resources:</label> {{dataItem.supportingResources}}
            </div>
            <div class="col-12 col-lg-4 evenItem">
                <label># of Measurements:</label> {{dataItem.measureCount}}
            </div>
        </div>`,
    filters: {
        formatDate: function (value): string {
            if (value)
                return kendo.format(DATE_FORMAT, value);

            return "";
        }
    }
});

const DATE_FORMAT = "{0:yyyy-MM-dd}";
const measuresVue = new Vue({
    el: "#vue_measures",
    data: function () {
        return {
            measures: <MeasureMetadata[]>[],
            cellTemplate: componentInstance,
            expandedItems: [],
            columns: [
                { field: 'metricTitle', title: 'Metric' },
                { field: 'runDateLocal', title: 'Run Date', format: DATE_FORMAT },
                { field: 'submittedOnLocal', title: 'Submitted On', format: DATE_FORMAT, type: "date" },
                { field: 'suspendedOnLocal', title: 'Suspended On', format: DATE_FORMAT }
            ],
            scrollable: 'scrollable',
            sort: [
                <SortDescriptor>{ field: 'submittedOn', dir: 'desc' }
            ],
            sortable: {
                allowUnsort: false,
                mode: 'single'
            },
            gridStyle: "maxHeight:900px;min-width:20rem;",
            loading: true
        }
    },
    components: {
        Grid,
        GridNoRecords
    },
    methods: {
        sortChangeHandler: function (e) {
            this.sort = e.sort;
        },
        expandChange: function (event) {
            Vue.set(event.dataItem, event.target.$props.expandField, event.value);
        }
    },
    mounted: function () {
        let self = this;
        axios.get<ApiResult<IMeasureMetadata[]>>('/api/measures/list')
            .then(function (response: AxiosResponse<ApiResult<IMeasureMetadata[]>>) {
                if (response.data.data) {
                    self.measures = response.data.data.map((value) => new MeasureMetadata(value));
                    self.loading = false;
                }
            });
    }
});

const visualizationVue = new Vue({
    el: "#vue_visualization",
    data: function () {
        return {
            visualizations: [],
            loading: true
        };
    },
    mounted: function () {
        let self = this;
        axios.get<any[]>('/api/visualization/bookmarks')
            .then(function (response: AxiosResponse) {
                self.visualizations = response.data;
                self.loading = false;
            });
    }
});

const favoriteMetricsVue = new Vue({
    el: "#vue_favorite_metrics",
    data: function () {
        return {
            metrics: [],
            loading: true
        };
    },
    mounted: function () {
        let self = this;
        axios.get<any[]>('/api/metrics/bookmarks')
            .then(function (response: AxiosResponse) {
                self.metrics = response.data;
                self.loading = false;
            });
    }
});