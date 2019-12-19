import Vue from 'vue';
import '@progress/kendo-ui';
import '@progress/kendo-theme-default/dist/all.css';

import { Dialog } from '@progress/kendo-dialog-vue-wrapper';
import { DialogInstaller } from '@progress/kendo-dialog-vue-wrapper';

import { Validator } from '@progress/kendo-validator-vue-wrapper';
import { ValidatorInstaller } from '@progress/kendo-validator-vue-wrapper';
import { debug } from 'util';

import axios from 'axios';
import { join } from 'path';
import { Http2ServerResponse } from 'http2';

Vue.use(DialogInstaller);
Vue.use(ValidatorInstaller);


Vue.component('password-score', {
    props: ['passwordValue'],
    data: function () {
        return {
            passwordScore: '',
            addedClasses: '',
        }
    },
    watch: {
        passwordValue: function (newPassword, oldpassword) {
            if (this.passwordValue == null || this.passwordValue.length == 0 || this.passwordValue.indexOf(":") > -1 || this.passwordValue.indexOf(";") > -1 || this.passwordValue.indexOf("<") > -1) {
                this.addedClasses = "bg-danger";
                this.passwordScore = 0 + "%";
                return;
            }

            if (this.passwordValue.length <= 4) {
                this.addedClasses = "bg-danger";
                this.passwordScore = 20 + "%";
            }

            var score = 1;

            if (this.passwordValue.length >= 8)
                score++;

            if (this.passwordValue.length >= 12)
                score++;

            var reg = /\d/;
            if (reg.test(this.passwordValue))
                score++;

            reg = /^(?=.*[a-z])(?=.*[A-Z]).+$/;
            if (reg.test(this.passwordValue))
                score++;

            reg = /[`,!,@,#,$,%,^,&,*,?,_,~,-,�,(,)]/;
            if (reg.test(this.passwordValue))
                score++;

            if (score <= 1) {
                this.addedClasses = "bg-danger";
                this.passwordScore = (score * 20) + "%";
            }
            else if (score < 5) {
                this.addedClasses = "bg-warning";
                this.passwordScore = (score * 20) + "%";

            }
            else if (score >= 5) {
                this.addedClasses = "bg-success";
                this.passwordScore = "100%";
            }

        }
    },
    template: '<div class="progress" style="margin-top: 3px;"><div class="progress-bar progress-bar-striped progress-bar-animated" role="progressbar" v-bind:class="addedClasses" v-bind:style="{width: passwordScore}"></div></div>',
});

export enum FormState {
    BeforeSubmit,
    DuringSubmit,
    SubmitSuccessfull
}

interface registrationVueDTO {
    firstName: string | null,
    lastName: string | null,
    email: string | null,
    phone: string | null,
    organizationRequested: string | null,
    roleRequested: string | null,
    username: string | null,
    password: string | null,
    confirmPassword: string |null,
    state: FormState,
    errorMessage: string | null,
    submitMeasures: boolean,
    submitMetrics: boolean
}

new Vue({
    el: '#vue_registration',
    data: <registrationVueDTO>{
        firstName: null,
        middleName: null,
        lastName: null,
        title: null,
        email: null,
        phone: null,
        fax: null,
        organizationRequested: null,
        roleRequested: null,
        username: null,
        password: null,
        confirmPassword: null,
        state: FormState.BeforeSubmit,
        errorMessage: null,
        submitMeasures: false,
        submitMetrics: false
    },
    methods: {
        resetUserProperties() {
            this.firstName = null;
            this.lastName = null;
            this.email = null;
            this.phone = null;
            this.organizationRequested = null;
            this.roleRequested = null;
            this.username = null;
            this.password = null;
            this.confirmPassword = null;
            this.submitMetrics = false;
            this.submitMeasures = false;
        },
        onRegister(e) {
            
            let validator = (<any>this).kendoValidator;
            if (this.confirmPassword != this.password) {
                this.errorMessage = "Please ensure that the passwords enter match.";
                return;
            }
            if (validator.validate()) {
                this.state = FormState.DuringSubmit;
                
                var dto = {
                    //Contact Information
                    firstName: this.firstName,
                    lastName: this.lastName,
                    email: this.email,
                    phone: this.phone,
                    organizationRequested: this.organizationRequested,
                    roleRequested: "",
                    username: this.username,
                    password: this.password,
                    confirmPassword: this.confirmPassword
                };

                if (this.submitMeasures) {
                    if (dto.roleRequested) {
                        dto.roleRequested += "; Submit Measures"
                    }
                    else {
                        dto.roleRequested = "Submit Measures"
                    }
                }

                if (this.submitMetrics) {
                    if (dto.roleRequested) {
                        dto.roleRequested += "; Submit Metrics"
                    }
                    else {
                        dto.roleRequested = "Submit Metrics"
                    }
                }

                //set empty objects to null to satisfy PMN validation
                for (let prp in Object.keys(dto)) {
                    let val = dto[prp];
                    if (val != null && val.trim().length == 0) {
                        dto[prp] = null;
                    }
                }

                axios.post('/api/Authentication/register', dto).then((response) => {
                    this.state = FormState.SubmitSuccessfull;
                    this.resetUserProperties();

                    }).catch((error) => {
                        this.state = FormState.BeforeSubmit;
                        let errors = error.response.data["error"][0];
                        this.errorMessage = errors;

                });
            }

            return false;
        },
        onCancel(e) {
            this.resetUserProperties();
            // Validation
            this.errorMessage = '';
            window.location.href = '/';
            return true;
        },
    }
});