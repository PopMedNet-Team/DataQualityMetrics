import Vue from 'vue';
import '@progress/kendo-ui';
import '@progress/kendo-theme-default/dist/all.css';
import axios from 'axios';
import { AutoComplete, ComboBox, DropDownList, MultiSelect, DropdownsInstaller } from '@progress/kendo-dropdowns-vue-wrapper';
import { Validator, ValidatorInstaller } from '@progress/kendo-validator-vue-wrapper';
import { Editor, EditorTool, EditorInstaller } from '@progress/kendo-editor-vue-wrapper';
import { Dialog, DialogInstaller } from '@progress/kendo-dialog-vue-wrapper';
import { Notification, KendoPopupsInstaller } from '@progress/kendo-popups-vue-wrapper';
import { debug } from 'util';
import $ from 'jquery';
import { IMetric, Metric, IMetricDependenciesResponse, IDomainItem, IResultsTypesItem, IDataQualtityFrameworkCategoryItem, DataQualityFrameworkCategoryItem, MetricDependenciesProvider, MetricStatuses } from './data-common';
import FileUpload from './Controls/FileUpload';
import FileList from './Controls/FileList';

Vue.component("file-upload", FileUpload);
Vue.component("file-list", FileList);
Vue.use(DropdownsInstaller);
Vue.use(ValidatorInstaller);
Vue.use(EditorInstaller);
Vue.use(DialogInstaller);
Vue.use(KendoPopupsInstaller);

let rootElement = document.getElementById('vue_metric_edit');
if (rootElement) {
    let metricID: string | null = rootElement.attributes['data-itemid'].value;

    Promise.all([
        axios.get<IMetric>('/api/metrics/' + metricID),
        axios.get<IMetricDependenciesResponse>('/api/metrics/dependencies')
    ]).then(values => {

        let metric = new Metric(values[0].data);
        let depends = new MetricDependenciesProvider(values[1].data);
        //don't need to have the dependencies track changes
        Object.freeze(depends);

        const toolbar = [
            'formatting','bold', 'italic', 'underline', 'strikethrough', 'subscript', 'superscript',
            'fontSize', 'foreColor', 'backColor', 'justifyLeft', 'justifyCenter', 'justifyRight', 'justifyFull',
            'insertUnorderedList', 'insertOrderedList', 'indent', 'outdent',
            'createLink', 'unlink',
            'tableWizard', 'createTable', 'addColumnLeft', 'addColumnRight', 'AddRowAbove', 'AddRowBelow',
            'deleteRow', 'deleteColumn','cleanFormatting'
        ];
        Object.freeze(toolbar);

        let pasteCleanup = {
            all: false,
            css: false,
            keepNewLiens: true,
            msAllFormatting: true,
            msConvertLists: true,
            msTags: true,
            none: false,
            span: false
        };
        Object.freeze(pasteCleanup);

        let vue = new Vue({
            el: '#vue_metric_edit',
            data: function () {
                return {
                    metric: metric,
                    dependencies: depends,
                    toolbar: toolbar,
                    pasteCleanup: pasteCleanup,
                    canSubmitForReview: (metric.status.metricStatusID === MetricStatuses.Draft || metric.status.metricStatusID === MetricStatuses.Rejected),
                    canDelete: metric.status.metricStatusID === MetricStatuses.Draft,
                    kendoOptions: {
                        appendTo: '#notification-container',
                        autoHideAfter: 5000,
                        postitionTop: 10,
                        width: 150
                    },
                    validationRules: {
                        rules: {
                            requiredEditor(input) {
                                let editor:any = null;
                                if (input.is("[name='txtDescription']")) {
                                    editor = (<any>vue.$refs.description_editor).kendoWidget();
                                } else if (input.is("[name='txtJustification']")) {
                                    editor = (<any>vue.$refs.justification_editor).kendoWidget();
                                } else if (input.is("[name='txtExpectedResults']")) {
                                    editor = (<any>vue.$refs.expectedResults_editor).kendoWidget();
                                }

                                if (editor) {
                                    let value: string = "<div>" + <string>(editor.value() || "") + "</div>";
                                    value = $(value).text().trim();
                                    return value.length > 0;
                                }

                                if (input.is("[name='cmbDomains']")) {
                                    editor = (<any>vue.$refs.domains_multiselect).kendoWidget();
                                    return editor.value().length > 0;
                                }

                                if (input.is("[name='cmbFrameworkCategories']")) {
                                    editor = (<any>vue.$refs.harmonization_category_multiselect).kendoWidget();
                                    return editor.value().length > 0;
                                }

                                return true;
                            }
                        }
                    }
                };
            },
            components: {
                Validator
            },
            mounted: function () {
                //@ts-ignore;
                this.saveNotificationWidget = this.$refs.saveNotification.kendoWidget();
            },
            methods: {
                onSave: function (event) {                    
                    let description_editor = (<any>this.$refs.description_editor).kendoWidget();
                    let justification_editor = (<any>this.$refs.justification_editor).kendoWidget();
                    let expectedResults_editor = (<any>this.$refs.expectedResults_editor).kendoWidget();
                    let domains_multiselect = (<any>vue.$refs.domains_multiselect).kendoWidget();
                    let harmonization_category_multiselect = (<any>vue.$refs.harmonization_category_multiselect).kendoWidget();

                    let validator = (<any>this).kendoValidator;
                    if (!validator.validate()
                        || !validator.validateInput($(description_editor.element[0]))
                        || !validator.validateInput($(justification_editor.element[0]))
                        || !validator.validateInput($(expectedResults_editor.element[0]))
                        || !validator.validateInput($(domains_multiselect.element[0]))
                        || !validator.validateInput($(harmonization_category_multiselect.element[0]))) {
                        return false;
                    }

                    this.metric.description = description_editor.value();
                    this.metric.justification = justification_editor.value();
                    this.metric.expectedResults = expectedResults_editor.value();                    

                    axios.post<Metric>('/api/metrics/' + metricID, this.metric)
                        .then((response) => {
                            window.location.href = '/metric/' + this.metric.id;
                            return true;
                        });
                },
                onDeleteMetric: function (event) {
                    //@ts-ignore;
                    kendo.confirm("Are you sure that you wish to delete this Metric?").then(function () {
                            axios.post('/api/metrics/delete/' + metric.id)
                                .then((response) => {
                                    window.location.href = '/metrics/';
                                    return true;
                                });
                        },
                        function () {
                            return false;
                        });
                },
                onShowSaveNotification: function (e) {
                    let el = e.element[0];
                    el.style.marginBottom = '15px';
                },
                onEditorChange: function (e) {       
                    //need to manually call validate when the editor blurs for custom validation to run
                    let validator = (<any>this).kendoValidator;
                    return validator.validateInput($(e.sender.element));
                }
            }
        });


    });
}