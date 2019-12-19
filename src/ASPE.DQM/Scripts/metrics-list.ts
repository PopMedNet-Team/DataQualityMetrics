import axios from 'axios';
import Vue from 'vue';
import '@progress/kendo-ui';
import '@progress/kendo-theme-default/dist/all.css';
import { orderBy, SortDescriptor } from '@progress/kendo-data-query';
import { Grid, GridNoRecords } from '@progress/kendo-vue-grid';
import { IMetricListItem, MetricStatuses, UserProfile } from './data-common';
import $ from 'jquery';

Vue.component('Grid', Grid);

interface DataModel {
    columns: any[];
    scrollable: string;
    sort: any[];
    sortable: any;
}

Promise.all([
    axios.get<IMetricListItem[]>('/api/metrics/list'),
    axios.get<UserProfile>('/api/authentication/profile')
]).then((responses) => {
    const response = responses[0];
    const metrics = <IMetricListItem[]>response.data;
    const user = <UserProfile>responses[1].data;
    
    let vue = new Vue({
        el: '#vue_metrics',
        data: function (): DataModel {
            return {
                columns: [
                    { field: 'title', title: 'Metric', cell: 'metricDetailsColumn' },
                    { field: 'frameworkCategories', title: 'DQ Harmonization Category', cell: (<any>this).renderFrameworkCategoriesColumn },
                    { field: 'domains', title: 'Domain', cell: (<any>this).renderDomainsColumn },
                    { field: 'status', title: 'Metric Status', cell: (<any>this).renderMetricStatusColumn, hidden: user.isSystemAdministrator },
                    { field: 'resultsType', title: 'Result Type', cell: (<any>this).renderResultsTypeColumn },
                    { field: 'author', title: 'Author', cell: (<any>this).renderAuthorColumn },
                    { field: 'serviceDeskUrl', title: 'JIRA # for public comments', cell: 'supportLinkColumn' }
                ],
                scrollable: 'false',
                sort: [
                    <SortDescriptor>{ field: 'title', dir: 'asc' }
                ],
                sortable: {
                    allowUnsort: false,
                    mode: 'single'
                }
            };
        },
        mounted: function () {
            if (user.canAuthorMetric == false && user.isSystemAdministrator == false) {
                this.columns.splice(3, 1);
            }
            $("#PageLoadingMessage").remove();
        },
        computed: {
            result: function (): IMetricListItem[] {
                let dir = this.sort[0].dir;
                if (this.sort[0].field == 'frameworkCategories') {

                    let sorted = metrics.sort(function (a, b) {
                        let f1 = (a.frameworkCategories || []);
                        let f2 = (b.frameworkCategories || []);

                        if (f1.length == 0 || f2.length == 0) {
                            return dir === 'asc' ? f1.length - f2.length : f2.length - f1.length;
                        }

                        let titleA = (f1 || []).map((c) => c.subCategory.length > 0 ? c.title + '-' + c.subCategory : c.title).join('; ')
                        let titleB = (f2 || []).map((c) => c.subCategory.length > 0 ? c.title + '-' + c.subCategory : c.title).join('; ')

                        return dir === 'asc' ? titleA.localeCompare(titleB) : titleB.localeCompare(titleA);
                    });

                    return sorted;

                } else if (this.sort[0].field == 'author') {

                    let sorted = metrics.sort(function (a, b) {
                        // @ts-ignore
                        if ((a.author == null && b.author == null) || (a.author.id == b.author.id)) {
                            return 0;
                        } else if (a.author == null && b.author != null) {
                            return dir === 'asc' ? -1 : 1;
                        } else if (a.author != null && b.author == null) {
                            return dir === 'asc' ? 1 : -1;
                        } else {
                            // @ts-ignore
                            return dir === 'asc' ? (a.author.firstName + ' ' + a.author.lastName).localeCompare(b.author.firstName + ' ' + b.author.lastName) : (b.author.firstName + ' ' + b.author.lastName).localeCompare(a.author.firstName + ' ' + a.author.lastName);
                        }

                    });

                    return sorted;

                } else if (this.sort[0].field == 'resultsType') {
                    return metrics.sort((a, b) => {
                        // @ts-ignore
                        return dir === 'asc' ? a.resultsType.value.localeCompare(b.resultsType.value) : b.resultsType.value.localeCompare(a.resultsType.value);
                    });
                }

                return orderBy(metrics, this.sort);
            }
        },
        methods: {
            renderAuthorColumn: function (h, tdElement, props, clickHandler) {
                return h('td', [(props.dataItem.author.firstName + ' ' + props.dataItem.author.lastName).trim()]);
            },
            renderFrameworkCategoriesColumn: function (h, tdElement, props, clickHandler) {
                let categories = (props.dataItem.frameworkCategories || []).map((c) => c.subCategory.length > 0 ? c.title + '-' + c.subCategory : c.title).join('; ');
                return h('td', [categories]);
            },
            renderDomainsColumn: function (h, tdElement, props, clickHandler) {
                let domains = (props.dataItem.domains || []).map((c) => c.title).join('; ');
                return h('td', [domains]);
            },
            renderMetricStatusColumn: function (h, tdElement, props, clickHandler) {
                return h('td', [props.dataItem.status.title]);
            },
            renderResultsTypeColumn: function (h, tdElement, props, clickHandler) {
                let resultsType = props.dataItem.resultsType.value;
                return h('td', [resultsType]);
            },
            sortChangeHandler: function (e) {
                this.sort = e.sort;
            },
            getMetricUrl: function (metric: IMetricListItem): string {
                //TODO: appropriately render the cell content to allow for access directly to edit as well as display based on user and permissions and metric state
                //if (metric.status.metricStatusID == MetricStatuses.Draft && metric.author.id == user.id) {
                //    return '/metric/' + metric.id + '/edit';
                //}
                return '/metric/' + metric.id;
            }
        }
    });

}).catch((error) => {
    debugger;
});