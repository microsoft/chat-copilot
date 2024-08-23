import { makeStyles } from '@fluentui/react-components';
import React, { useMemo } from 'react';
import Carousel from 'react-multi-carousel';
import 'react-multi-carousel/lib/styles.css';
import { ISpecialization } from '../../libs/models/Specialization';
import { useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { SpecializationCard } from './SpecializationCard';

const responsive = {
    // Define responsive settings for different screen sizes
    desktop: {
        breakpoint: { max: 3000, min: 1024 },
        items: 2,
        slidesToSlide: 2,
    },
    tablet: {
        breakpoint: { max: 1024, min: 464 },
        items: 2,
        slidesToSlide: 2,
    },
    mobile: {
        breakpoint: { max: 464, min: 0 },
        items: 1,
        slidesToSlide: 1,
    },
};

const useClasses = makeStyles({
    carouselroot: {
        width: '700px',
    },
    // Allows for the carousel to be centered when there is only one item
    carouselrootSingleItem: {
        width: '700px',
        display: 'flex',
        justifyContent: 'center',
    },
    innercontainerclass: {
        height: '330px',
    },
    innertitle: {
        textAlign: 'center',
    },
});

interface SpecializationProps {
    specializations: ISpecialization[];
}

/**
 * Renders the SpecializationCardList Carousel component.
 *
 * Note: Dynamic styling to handle the case when there is only one specialization.
 *
 * @param {{specializations: ISpecialization[]}} - List of specializations
 * @returns {*} Specialization Carousel select component
 */
export const SpecializationCardList: React.FC<SpecializationProps> = ({ specializations }) => {
    const classes = useClasses();
    const { activeUserInfo } = useAppSelector((state: RootState) => state.app);

    // Memoize the filtered specializations based on the user's group memberships
    const filteredSpecializations = useMemo(() => {
        return specializations.filter((_specialization) => {
            const hasMembership =
                activeUserInfo?.groups.some((val) => {
                    return _specialization.groupMemberships.includes(val);
                }) ?? false;

            // Check if the user has membership to the specialization group and the specialization is active
            const canViewSpecialization =
                ((hasMembership || _specialization.groupMemberships.length === 0) && _specialization.isActive) ||
                _specialization.id == 'general';

            return canViewSpecialization ? _specialization : undefined;
        });
    }, [activeUserInfo?.groups, specializations]);

    return (
        <>
            <h1 className={classes.innertitle}>Choose Specialization</h1>
            <Carousel
                className={filteredSpecializations.length === 1 ? classes.carouselrootSingleItem : classes.carouselroot}
                responsive={responsive}
                showDots={true}
                swipeable={true}
                arrows={true}
                dotListClass="custom-dot-list-style"
            >
                {filteredSpecializations.map((_specialization, index) => (
                    <div key={index} className={classes.innercontainerclass}>
                        <SpecializationCard key={_specialization.id} specialization={_specialization} />
                    </div>
                ))}
            </Carousel>
        </>
    );
};
