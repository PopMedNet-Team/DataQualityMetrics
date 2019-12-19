import axios from 'axios';
import Vue from 'vue';
import $ from 'jquery';
import '@progress/kendo-ui';
import '@progress/kendo-theme-default/dist/all.css';
import { formatDate } from '@telerik/kendo-intl';
import { orderBy, SortDescriptor } from '@progress/kendo-data-query';
import { Grid, GridNoRecords } from '@progress/kendo-vue-grid';
import { ApiResult } from './data-common';
import { debug } from 'webpack';

interface IMeasureItem {
    id: any;
    submittedByID: any;
    submittedBy: string;
    submittedOn: any;
    suspendedByID: any | null;
    suspendedOn: any | null;
    organization: string | null;
    dataSource: string | null;
    runDate: any;
    metricTitle: string;
}

class MeasureItem {

    constructor(
        readonly id: any,
        readonly submittedByID: any,
        readonly submittedBy: string,
        readonly submittedOn: any,
        readonly suspendedByID: any | null,
        readonly suspendedOn: any | null,
        readonly organization: string | null,
        readonly dataSource: string | null,
        readonly runDate: any,
        readonly metricTitle: string
    ) { }
}



const CommandCell = Vue.component("template-component", {
    props: {
        field: String,
        dataItem: Object,
        format: String,
        className: String,
        columnIndex: Number,
        columnsCount: Number,
        rowType: String,
        level: Number,
        expanded: Boolean,
        editor: String
    },
    template: `<td><button class="k-primary k-button k-grid-edit-command mb-1" @click="suspendHandler">{{ this.dataItem.suspendedByID == null ? 'Suspend' : 'Activate'}}</button> <button class="k-secondary k-button k-grid-edit-command" @click="deleteItemHandler"><i class="fas fa-trash"></i>&nbsp;Delete</button></td>`,
    methods: {
        suspendHandler: function () {
            //using the edit command to represent "suspend" since only the predined commands are propagated through the component
            this.$emit('edit', this.dataItem );
        },
        deleteItemHandler: function () {
            let measure = this.dataItem;
            let self = this;
            (<any>kendo.confirm("Are you sure you wish to delete this set of Measures?")).then(function () {
                self.$emit('remove', measure );
            }, function () {
                    //user canceled deleting
                });
            }        
    }
});

interface DataModel {
    measures: IMeasureItem[];
    columns: any[];
    scrollable: string;
    sort: any[];
    sortable: any;
    gridStyle: string;
}

axios.get<ApiResult<IMeasureItem[]>>('/api/measures/list?forAdmin=true')
    .then((response) => {
        const measures = response.data;
        const DATE_FORMAT = "{0:yyy-MM-dd}";

        const mapMeasureItem = function (m: IMeasureItem[]) {
            return m.map(i => new MeasureItem(i.id, i.submittedByID, i.submittedBy, new Date(i.submittedOn), i.suspendedByID, i.suspendedOn ? new Date(i.suspendedOn) : null, i.organization, i.dataSource, new Date(i.runDate), i.metricTitle));
        };

        let vue = new Vue({
            el: "#vue_mangage_measures",
            data: function (): DataModel {
                return {
                    measures: mapMeasureItem(measures.data || []),
                    columns: [
                        { field: 'submittedBy', title: 'Submitted By' },
                        { field: 'submittedOn', title: 'Submitted On', format: DATE_FORMAT, type:"date" },
                        { field: 'suspendedOn', title: 'Suspended On', format: DATE_FORMAT },
                        { field: 'organization', title: 'Organization' },
                        { field: 'dataSource', title: 'DataSource' },
                        { field: 'runDate', title: 'Run Date', format: DATE_FORMAT },
                        { field: 'metricTitle', title: 'Metric' },
                        { cell: CommandCell, width: '100px'}
                    ],
                    scrollable: 'scrollable',
                    sort: [
                        <SortDescriptor>{ field: 'submittedOn', dir: 'desc' }
                    ],
                    sortable: {
                        allowUnsort: false,
                        mode: 'single'
                    },
                    gridStyle: "maxHeight:900px;"
                };
            },
            components: {
                Grid,
                GridNoRecords,
                CommandCell
            },
            computed: {
                result: function (): IMeasureItem[] {
                    return orderBy(this.measures, this.sort);
                }
            },
            methods: {
                sortChangeHandler: function (e) {
                    this.sort = e.sort;
                },
                edit(e) {
                    let self = this;
                    //using the edit command to represent "suspend" since only the predined commands are propagated through the component
                    let id = e.dataItem.id;
                    axios.put('/api/measures/toggle/'+ id).then((response) => {
                        self.refreshItems();
                    });
                },
                remove (e) {
                    let self = this;
                    //using the edit command to represent "suspend" since only the predined commands are propagated through the component
                    let id = e.dataItem.id;
                    axios.delete('/api/measures/delete/' + id).then((response) => {                        
                        self.refreshItems();
                    });
                },
                refreshItems() {
                    let self = this;
                    axios.get<ApiResult<IMeasureItem[]>>('/api/measures/list?forAdmin=true')
                        .then((response) => {
                            self.measures = mapMeasureItem(response.data.data || []);
                        });
                }
            },
            mounted: function () {
                $("#PageLoadingMessage").remove();
            }
        });


    });