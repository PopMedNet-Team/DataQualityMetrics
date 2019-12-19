import Vue from 'vue';
import axios from 'axios';
import { debug } from 'webpack';
import '@progress/kendo-ui';
import '@progress/kendo-theme-default/dist/all.css';
import { AutoComplete, ComboBox, DropDownList, MultiSelect, DropdownsInstaller } from '@progress/kendo-dropdowns-vue-wrapper';
import { IMetric, Metric, IMetricDependenciesResponse, MetricDependenciesProvider, IDocument, MetricStatuses } from './data-common';
import $ from 'jquery';

Vue.use(DropdownsInstaller);

let rootElement = document.getElementById('vue_metric_details');
if (rootElement) {
    let metricID: string | null = rootElement.attributes['data-itemid'].value;

    Promise.all([
        axios.get<IMetric>('/api/metrics/' + metricID),
        axios.get<IMetricDependenciesResponse>('/api/metrics/dependencies'),
        axios.get('/api/Documents/List/' + metricID)
    ]).then(values => {

        let metric = new Metric(values[0].data);
        let depends = new MetricDependenciesProvider(values[1].data);
        let docs: IDocument = values[2].data.map(item => {
            return <IDocument>{
                removeLink: '<button type="button" class="btn btn-danger" @click="removeDocument(doc.id)">Remove</button>',
                nameLink: '<a href="/api/Documents/Download/' + item.id + '"><i class="far fa-file"></i> ' + item.name + '</a>',
                id: item.id,
                name: item.name,
                size: item.size,
                mimeType: item.mimeType,
                createdOn: item.createdOn,
                itemID: item.itemID,
                userName: item.userName,
                firstName: item.firstName,
                lastName: item.LastName
            }
        });

        let nextStatuses: { text: string, value: any }[] = [];
        switch (metric.status.metricStatusID) {
            case MetricStatuses.Draft:
                nextStatuses.push({ text: 'Submitted', value: MetricStatuses.Submitted });
                break;
            case MetricStatuses.Submitted:
                nextStatuses.push({ text: 'In Review', value: MetricStatuses.InReview });
                nextStatuses.push({ text: 'Published', value: MetricStatuses.Published });
                nextStatuses.push({ text: 'Published - requires authentication', value: MetricStatuses.PublishedRequiresAuthentication });
                nextStatuses.push({ text: 'Rejected', value: MetricStatuses.Rejected });
                break;
            case MetricStatuses.InReview:
                nextStatuses.push({ text: 'Published', value: MetricStatuses.Published });
                nextStatuses.push({ text: 'Published - requires authentication', value: MetricStatuses.PublishedRequiresAuthentication });
                nextStatuses.push({ text: 'Rejected', value: MetricStatuses.Rejected });
                nextStatuses.push({ text: 'Inactive', value: MetricStatuses.Inactive });
                nextStatuses.push({ text: 'Deleted', value: MetricStatuses.Deleted });
                break;
            case MetricStatuses.Published:
                nextStatuses.push({ text: 'Published - requires authentication', value: MetricStatuses.PublishedRequiresAuthentication });
                nextStatuses.push({ text: 'Rejected', value: MetricStatuses.Rejected });
                nextStatuses.push({ text: 'Inactive', value: MetricStatuses.Inactive });
                nextStatuses.push({ text: 'Deleted', value: MetricStatuses.Deleted });
                break;
            case MetricStatuses.PublishedRequiresAuthentication:
                nextStatuses.push({ text: 'Published', value: MetricStatuses.Published });
                nextStatuses.push({ text: 'Rejected', value: MetricStatuses.Rejected });
                nextStatuses.push({ text: 'Inactive', value: MetricStatuses.Inactive });
                nextStatuses.push({ text: 'Deleted', value: MetricStatuses.Deleted });
                break;
            case MetricStatuses.Rejected:
                nextStatuses.push({ text: 'Published', value: MetricStatuses.Published });
                nextStatuses.push({ text: 'Published - requires authentication', value: MetricStatuses.PublishedRequiresAuthentication });
                nextStatuses.push({ text: 'Inactive', value: MetricStatuses.Inactive });
                nextStatuses.push({ text: 'Deleted', value: MetricStatuses.Deleted });
                break;
            case MetricStatuses.Inactive:
                nextStatuses.push({ text: 'Published', value: MetricStatuses.Published });
                nextStatuses.push({ text: 'Published - requires authentication', value: MetricStatuses.PublishedRequiresAuthentication });
                nextStatuses.push({ text: 'Deleted', value: MetricStatuses.Deleted });
                break;
        }

        //don't need to have the dependencies track changes
        Object.freeze(depends);
        Object.freeze(docs);
        Object.freeze(nextStatuses);


        let vue = new Vue({
            el: '#vue_metric_details',
            data: function () {
                return {
                    metric: metric,
                    selectedDomains: metric.domains,
                    dependencies: depends,
                    documents: docs,
                    nextStatusId: nextStatuses.length > 0 ? nextStatuses[0].value : null,
                    nextStatusComment: '',
                    nextStatuses: nextStatuses
                };
            },
            computed: {
                resultsTypeTitle: function () : string {
                    return this.dependencies.resultsTypes.filter(r => r.id == this.metric.resultsTypeID)[0].value;
                },
                frameworkCategoriesList: function () : string {
                    let categories = (this.metric.frameworkCategories || []).map(i => {
                        return this.dependencies.frameworkCategories.filter(f => f.id == i)[0].displayTitle;
                    });

                    return categories.join('; ');
                },
                domains: function () : string {
                    let dom = (this.metric.domains || []).map(i => {
                        return this.dependencies.domains.filter(f => f.id == i)[0].title;
                    });

                    return dom.join('; ');
                }
            },
            methods: {
                onCopy: function (event) {
                    axios.post('/api/metrics/copy/' + metric.id)
                        .then((response) => {
                            window.location.href = '/metric/' + response.data + '/edit';
                            return true;
                        });
                },
                onUpdateStatus: function (event) {
                    axios.post<any>('/api/metrics/update-status', { metricID: metricID, newStatusID: this.nextStatusId, comment: this.nextStatusComment })
                        .then((response) => {
                            window.location.reload();
                        });
                },
                onSubmitForReview: function (event) {
                    axios.post<any>('/api/metrics/update-status', { metricID: metricID, newStatusID: MetricStatuses.Submitted, comment: '' })
                        .then((response) => {
                            window.location.reload();
                        });
                },
                onToggleBookmark: function (event) {                    
                    axios.post('/api/metrics/bookmark/' + metricID)
                        .then((response) => {
                            metric.bookmarked = !metric.bookmarked;
                        });
                }
            },
            mounted: function () {
                $("#PageLoadingMessage").remove();
            }
        });


    });
}
Vue.filter("formatfilesize", function (length: number) {
    let kilobyte: number = 1024;
    let megabyte: number = 1024 * 1024;
    let gigabyte: number = 1024 * 1024 * 1024;
    if (length > gigabyte)
        return (length / gigabyte).toFixed(2) + ' Gb';

    if (length > megabyte)
        return (length / megabyte).toFixed(2) + ' Mb';

    if (length > kilobyte)
        return (length / kilobyte).toFixed(2) + ' Kb';

    return length + ' bytes';
});